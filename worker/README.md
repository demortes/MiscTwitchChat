# MiscTwitchChat Worker

The Cloudflare Workers version of the API (Hono + TypeScript + D1). It also receives Twitch chat activity through
an EventSub webhook, so no long-running bot is needed.

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
npx wrangler secret put DISCORD_PUBLIC_KEY
npx wrangler secret put DISCORD_APP_ID
npm run deploy
```

Twitch setup is in [docs/twitch-eventsub.md](docs/twitch-eventsub.md). Discord slash commands (a handy way to try
the API without Twitch chat) are in [docs/discord.md](docs/discord.md).

## Deploy from GitHub Actions

[`.github/workflows/worker.yml`](../.github/workflows/worker.yml) typechecks, tests and dry-run bundles the Worker on
every pull request and push that touches `worker/`. Pushes to `main` then apply the D1 migrations and deploy.

Add three repository secrets (Settings > Secrets and variables > Actions):

| Secret | Value |
| --- | --- |
| `CLOUDFLARE_API_TOKEN` | A Cloudflare API token that can edit Workers scripts and D1 databases on your account |
| `CLOUDFLARE_ACCOUNT_ID` | Your Cloudflare account ID |
| `CLOUDFLARE_D1_DATABASE_ID` | The `database_id` from `wrangler d1 create` |

The workflow builds `wrangler.jsonc` from `wrangler.example.jsonc` using that database ID, so no identifier is ever
committed. Without these secrets (for example on a fork) the tests still run and the deploy is skipped. Runtime
secrets such as `OPEN_AI_KEY` are set once with `wrangler secret put` and persist across deploys.

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
