# Twitch EventSub setup

The Worker replaces the IRC bot's chat logging. Twitch POSTs `channel.chat.message`
notifications to `POST /twitch/eventsub`; each one upserts `active_chatters`
(`channel` = broadcaster login, `username` = chatter login, both lowercase).
Writes are skipped if the same chatter was written less than 60 s ago (D1 write budget).

## Secrets

    wrangler secret put TWITCH_EVENTSUB_SECRET   # any random string, 10-100 chars

The subscribe script asks for what it needs when you run it in a terminal (secrets are not echoed):
the Twitch app's client ID and secret, `TWITCH_EVENTSUB_SECRET` (same value as the Worker), the
callback URL (`https://<your-worker-domain>/twitch/eventsub`, HTTPS on port 443) and the bot account
(its login name or numeric user id). To run it without prompts, set these as environment variables
instead: `TWITCH_CLIENT_ID`, `TWITCH_CLIENT_SECRET`, `TWITCH_EVENTSUB_SECRET`, `TWITCH_CALLBACK_URL`,
`TWITCH_BOT_USER_ID`. Anything already set is not asked for.

## Authorization (scopes)

Creating a `channel.chat.message` webhook subscription uses an app access token. Twitch then requires
all of the following, otherwise it answers "subscription missing proper authorization":

- the bot account (`condition.user_id`) has authorized your app with **both** `user:read:chat` and `user:bot`, and
- for each channel you subscribe to, **either** the broadcaster has authorized your app with `channel:bot`
  **or** the bot account is a moderator in that channel.

1. Logged in as the bot account (a private window helps), open this with your app's redirect URI
   registered under the app's OAuth Redirect URLs:
   `https://id.twitch.tv/oauth2/authorize?client_id=<ID>&redirect_uri=<URI>&response_type=code&scope=user%3Aread%3Achat%20user%3Abot`
   and approve. The resulting user token is not needed by the Worker; the authorization itself is
   what Twitch checks when the subscription is created. (The redirect may show an error page if the
   URI is not a real site, which is fine.)
2. For each channel, either make the bot a moderator (`/mod <botname>` in that channel), or have the
   broadcaster open the same URL with `scope=channel%3Abot` while logged in as the broadcaster.

## Self-service: "Add my Twitch bot to your channel"

The site footer links to `/twitch/connect`. A broadcaster who clicks it authorizes your app with `channel:bot`
on Twitch, is sent back to `/twitch/callback`, and the Worker then creates the `channel.chat.message`
subscription for that channel. Their token is used once to find out who they are and is not stored.

Set it up once:

1. Add `https://<your-worker-domain>/twitch/callback` to your app's **OAuth Redirect URLs** at
   dev.twitch.tv/console (one entry per domain you use, for example the custom domain).
2. Give the Worker the app's credentials and the bot account (its numeric user id, or its login name):
   `wrangler secret put TWITCH_CLIENT_ID`, `TWITCH_CLIENT_SECRET` and `TWITCH_BOT_USER_ID`
   (plus `TWITCH_EVENTSUB_SECRET`, as above). Without them `/twitch/connect` returns 404.
3. The bot account must have authorized your app with `user:read:chat` and `user:bot` (the "Authorization"
   section above). This is a one-time step for the bot, not for each channel.

Anyone with a Twitch account can use the page, so channels sign themselves up. Remove a channel's
subscription with `node scripts/twitch-subscribe.ts --list` and `--delete <id>`.

## Running the script (Node >= 22.18; older 22.x: add `--experimental-strip-types`)

    cd worker
    node scripts/twitch-subscribe.ts channel1 channel2
    node scripts/twitch-subscribe.ts --list
    node scripts/twitch-subscribe.ts --delete <subscription-id>   # or: --delete all

Deploy the Worker with the secret set before subscribing: Twitch immediately sends a
`webhook_callback_verification` request that the Worker must answer.

## Notes

- Signature: `sha256=` + HMAC-SHA256(secret, message-id + timestamp + raw body); a bad signature
  or a timestamp older than 10 minutes gets 403.
- Duplicate deliveries are deduped per isolate by message id (in-memory, best effort); the upsert
  is idempotent regardless.
- Revoked or repeatedly failing subscriptions are disabled by Twitch; re-run the script to recreate.
