# Discord interactions endpoint

The Worker replaces the old Discord.Net gateway bot. Discord POSTs every slash command to
`POST /discord/interactions`; each request is verified with Ed25519 against `DISCORD_PUBLIC_KEY`
(401 on failure) and PING is answered with PONG.

## Setup

1. In the [Discord Developer Portal](https://discord.com/developers/applications), create an application (or open
   yours). On **General Information** copy the **Application ID** and the **Public Key**. On the **Bot** tab, use
   **Reset Token** to get a bot token (shown once). Leave the privileged intents off.
2. Give the Worker its secrets (from `worker/`). Nothing Discord-specific is stored in the repo; it all lives on
   the instance:
   ```
   npx wrangler secret put DISCORD_PUBLIC_KEY   # verifies request signatures
   npx wrangler secret put DISCORD_APP_ID       # used by the site's "Add my Discord bot" link (/discord/invite)
   ```
   The Worker itself never reads the bot token. Only the register script in step 4 needs it, and Cloudflare secrets
   cannot be read back, so supply it from your shell for that one command and never commit it. For local dev put
   the two secrets above in `.dev.vars`.
3. Deploy (`npm run deploy`), then in the portal set **Interactions Endpoint URL** to
   `https://<your-worker-domain>/discord/interactions`. Discord sends a signed PING; it is only
   accepted if the endpoint answers correctly, so deploy the secrets first.
4. Register the commands (bulk overwrite, so removed commands disappear):
   ```
   DISCORD_APP_ID=... DISCORD_BOT_TOKEN=... npx vite-node scripts/register-commands.ts
   # fast, guild-scoped (instant instead of up to ~1h for global):
   DISCORD_GUILD_ID=... DISCORD_APP_ID=... DISCORD_BOT_TOKEN=... npx vite-node scripts/register-commands.ts
   ```
5. Invite the application to your server by opening `https://<your-worker-domain>/discord/invite`. It redirects to
   Discord's authorize page for your application with only the `applications.commands` scope (no `bot` scope,
   permissions or gateway intents are needed any more).

## Commands

`blame`, `blamedem`, `blamecody`, `ping`, `userinfo`, `cah`, `bancah`, `cat`, `dad`, `dog`, `insult`,
`generate-name`, `roll` — defined once in `src/discord/commands.ts` (shared by the router and the register script).

- Fast commands (`ping`, `roll`, `userinfo`, `blame*`) reply inline.
- Commands calling the Worker's own API (`cat`, `dad`, `dog`, `insult`, `generate-name`, `cah`, `bancah`) reply with a
  deferred response and then PATCH `webhooks/{app}/{token}/messages/@original` via `waitUntil`. The API is called in-process
  (`app.fetch`), so no network hop or public URL is needed.
- `cah tts` posts a followup message with `tts: true` and deletes the "thinking" placeholder (edits cannot set TTS).
- Every message is truncated to 2000 characters and sent with `allowed_mentions: { parse: [] }`, so nothing can ping.

## Differences from the C# bot

- `/roll` now rolls 1..sides inclusive (C# used `Random.Next(1, sides)`, which excluded the top face) and is limited to
  1-100 dice and 1-1,000,000 sides; anything else gets the "incorrect parameters" reply.
- Option descriptions default to the option name (as Discord.Net did).
