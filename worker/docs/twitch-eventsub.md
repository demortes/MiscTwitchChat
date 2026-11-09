# Twitch EventSub setup

The Worker replaces the IRC bot's chat logging. Twitch POSTs `channel.chat.message`
notifications to `POST /twitch/eventsub`; each one upserts `active_chatters`
(`channel` = broadcaster login, `username` = chatter login, both lowercase).
Writes are skipped if the same chatter was written less than 60 s ago (D1 write budget).

## Secrets

    wrangler secret put TWITCH_EVENTSUB_SECRET   # any random string, 10-100 chars

For the subscribe script (local env, not Worker secrets): `TWITCH_CLIENT_ID`,
`TWITCH_CLIENT_SECRET`, `TWITCH_EVENTSUB_SECRET` (same value as the Worker),
`TWITCH_CALLBACK_URL` (`https://<your-worker-domain>/twitch/eventsub`, HTTPS on port 443),
`TWITCH_BOT_USER_ID` (numeric user id of the bot account).

## Authorization (scopes)

Creating a `channel.chat.message` webhook subscription uses an app access token, and the user in
`condition.user_id` (the bot account) must have authorized your app with scope `user:read:chat`.
(Alternative: the broadcaster grants `channel:bot`, or the bot is a moderator with `user:bot`.)

1. Logged in as the bot account, open (with your app's redirect URI registered):
   `https://id.twitch.tv/oauth2/authorize?client_id=<ID>&redirect_uri=<URI>&response_type=code&scope=user:read:chat`
2. Approve. The resulting user token is not needed by the Worker; the authorization itself is
   what Twitch checks when the subscription is created.

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
