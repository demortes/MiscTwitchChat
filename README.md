[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fdemortes%2FMiscTwitchChat.svg?type=shield)](https://app.fossa.com/projects/git%2Bgithub.com%2Fdemortes%2FMiscTwitchChat?ref=badge_shield)

# Misc Twitch Api

A small collection of endpoints that give a chat bot easy access to fun, canned or looked-up responses: random cat,
dog and dad facts, Cards Against Humanity cards, insults, hugs, pokes and spanks between chatters, dictionary and
Urban Dictionary lookups, AI-generated names, and more. Most endpoints return a single line of plain text, ready to
drop into chat.

It runs as a [Cloudflare Worker](https://developers.cloudflare.com/workers/) with a D1 database, so there are no
servers or containers to look after. Everything lives in [`worker/`](worker/).

| Piece | What it does |
| --- | --- |
| HTTP API (`/api/...`) | The chat-bot endpoints. Browse and try them in Swagger UI at `/swagger`. |
| Twitch EventSub webhook (`/twitch/eventsub`) | Records who is chatting, so hug, poke, spank and gift can pick a random active chatter. |
| Discord interactions (`/discord/interactions`) | The same commands as Discord slash commands, handy for trying the API without Twitch chat. |

## Getting started

See [`worker/README.md`](worker/README.md) for running your own copy, local development, secrets, and the GitHub
Actions deploy. Twitch and Discord setup are in [`worker/docs/`](worker/docs/).

All account-specific values (the D1 database ID, Cloudflare IDs, API keys, Discord and Twitch credentials) are kept
as secrets on your own deployment or in your repository's secrets, never in the repository.

## Health checks

| Endpoint | Reports |
| --- | --- |
| `/health` and `/health/live` | The Worker is running and answering. No dependencies are probed. |
| `/health/ready` | The D1 database is reachable. Returns 503 if it is not. |

## License

MIT. See [LICENSE](LICENSE).
