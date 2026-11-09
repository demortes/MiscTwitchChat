# Discord interactions endpoint

The Worker replaces the old Discord.Net gateway bot. Discord POSTs every slash command to
`POST /discord/interactions`; each request is verified with Ed25519 against `DISCORD_PUBLIC_KEY`
(401 on failure) and PING is answered with PONG.

## Setup

1. In the Discord Developer Portal, open your application. Copy the **Application ID**, **Public Key**
   (General Information) and the **Bot token** (Bot tab).
2. Set Worker secrets (from `worker/`):
   ```
   npx wrangler secret put DISCORD_PUBLIC_KEY
   npx wrangler secret put DISCORD_APP_ID
   npx wrangler secret put DISCORD_BOT_TOKEN   # only needed by the register script, not the Worker itself
   ```
   For local dev put them in `.dev.vars`.
3. Deploy (`npm run deploy`), then in the portal set **Interactions Endpoint URL** to
   `https://<your-worker-domain>/discord/interactions`. Discord sends a signed PING; it is only
   accepted if the endpoint answers correctly, so deploy the secrets first.
4. Register the commands (bulk overwrite, so removed commands disappear):
   ```
   DISCORD_APP_ID=... DISCORD_BOT_TOKEN=... npx vite-node scripts/register-commands.ts
   # fast, guild-scoped (instant instead of up to ~1h for global):
   DISCORD_GUILD_ID=... DISCORD_APP_ID=... DISCORD_BOT_TOKEN=... npx vite-node scripts/register-commands.ts
   ```
5. Invite the bot with the `applications.commands` scope (no gateway/bot intents needed anymore).

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
