# MiscTwitchChat Worker

The Cloudflare Workers version of the API (Hono + TypeScript + D1). It also receives Twitch chat activity through
an EventSub webhook, replacing the long-running `TwitchActivityBot`.

## Run your own copy

Everything account-specific lives in `wrangler.jsonc`, which is **gitignored**. Each deployment uses its own D1
database, so create yours rather than reusing anyone else's.

```bash
npm install
cp wrangler.example.jsonc wrangler.jsonc        # Windows PowerShell: Copy-Item wrangler.example.jsonc wrangler.jsonc
npx wrangler login
npx wrangler d1 create misctwitchchat            # prints a database_id: paste it into wrangler.jsonc
npm run db:migrate:remote                        # creates the tables in your D1 database
```

Set the secrets you need (each is optional; a route that needs a missing one returns an error):

```bash
npx wrangler secret put OPEN_AI_KEY
npx wrangler secret put OPEN_AI_URL
npx wrangler secret put TwitterApiKey
npx wrangler secret put TwitterApiSecret
npx wrangler secret put DictionaryApiKey
npx wrangler secret put TWITCH_EVENTSUB_SECRET
npm run deploy
```

Twitch setup is in [docs/twitch-eventsub.md](docs/twitch-eventsub.md).

## Develop locally

No Cloudflare account or network is needed for the tests or the local server.

```bash
npm run typecheck && npm test      # unit tests
npm run db:migrate:local           # local SQLite copy of the schema
npm run dev                        # http://127.0.0.1:8787
```

For local secrets, create `.dev.vars` (gitignored) with one `NAME=value` per line, and restart `npm run dev` after
changing it.

Never commit `wrangler.jsonc`, `.dev.vars`, API tokens or secret values.
