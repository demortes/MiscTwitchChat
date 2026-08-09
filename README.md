[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fdemortes%2FMiscTwitchChat.svg?type=shield)](https://app.fossa.com/projects/git%2Bgithub.com%2Fdemortes%2FMiscTwitchChat?ref=badge_shield)

# Misc Twitch Api
## Description
This project is purely made out of the desire to aggregate a number of functions together and allow a chat bot easy access to items.

## Health checks
All three programs expose the same three HTTP endpoints and ship with a Docker `HEALTHCHECK` that probes `/health/ready`.

| Endpoint | Reports |
| --- | --- |
| `/health/live` | The process is running and answering. No dependencies are probed. |
| `/health/ready` | Every dependency the service needs to do its job. |
| `/health` | Everything registered, including checks not required for readiness. |

Readiness dependencies per service:

| Service | Checks |
| --- | --- |
| MiscTwitchChat (API) | `mysql` |
| DiscordBot | `discord` (gateway connection), `api` |
| TwitchActivityBot | `mysql`, `twitch` (chat connection), `api` |

The response body is JSON naming each check, its status and how long it took, so a failing probe says *which* dependency broke:

```json
{"status":"Unhealthy","totalDurationMs":2040.9,"checks":[{"name":"mysql","status":"Unhealthy","description":"MiscTwitchDbContext failed to connect to the database.","durationMs":2035.3}]}
```

These endpoints are unauthenticated, so the response carries only descriptions written by this solution. Underlying exception detail — driver messages that can name the database user or host — is left out of the body and written to the log instead, at `Error` level under `Microsoft.Extensions.Diagnostics.HealthChecks`.

The bots have no web host of their own, so they start a small listener alongside their normal work. Configuration:

| Setting | Environment variable | Default |
| --- | --- | --- |
| `Health:Port` | `Health__Port` | `8080` |
| `Health:ApiHealthPath` | `Health__ApiHealthPath` | `health/live` |
| `Health:ApiTimeoutSeconds` | `Health__ApiTimeoutSeconds` | `5` |
| `BaseAPIUrl` | `BaseAPIUrl` | per `appsettings.json` |

The bots probe the API's `/health/live` rather than its `/health/ready`, so a database outage is reported once by the API instead of cascading into every bot as well. Leaving `BaseAPIUrl` empty disables the API check.

The container health check probes the URL in the `HEALTHCHECK_URL` environment variable, which defaults to `http://127.0.0.1:8080/health/ready`. Override it if you change the listener port, or, for the API, if you set `ASPNETCORE_URLS` to something other than the image's default port 8080.

### Known limitation
TwitchActivityBot detects the server version and runs migrations before it starts its health listener, so if MySQL is unreachable *at startup* the process exits before it can report anything and the container restarts instead of going unhealthy. Outages that begin once the bot is running are reported normally. The API has no such gap: it configures its database lazily, so it stays up and answers `/health/ready` with `Unhealthy` whether MySQL was missing at boot or failed later.

## Maintainers
The official maintainer is Kevin "Demortes" Dethlefs, a Senior Software Engineer, who provides his free time to do this. 

## License
All code here is maintained with the MIT License. This means users, developers or anyone in the world should not count on it to be maintained, unique or earth shattering. As with anything security based, assume all information gathered by the API is public domain. It is provided for free, and no data is exchanged with other companies unless required by law or to complete the action the user requested.


[![FOSSA Status](https://app.fossa.com/api/projects/git%2Bgithub.com%2Fdemortes%2FMiscTwitchChat.svg?type=large)](https://app.fossa.com/projects/git%2Bgithub.com%2Fdemortes%2FMiscTwitchChat?ref=badge_large)

## Support
No support is given for the API. Questions on how to consume or use the API will be ignored. Repeated questions will result in further actions, such as blocking by the maintainer, or if particularly malicious/harmful, reports to appropriate authorities. If you would like to notify the maintainers of possible issues, file an issue ticket on GitHub and anyone interested can pick up and resolve the concern. If there are security concerns, you may email Demortes at [webmaster@demortes.com](mailto://webmaster@demortes.com).

## Contribute
Anyone is welcome to fork and modify the code on their own. If you'd like to see your code in the main API, feel free to fork, modify the code and test, then submit a pull request to the original repository. There is no gaurenteed time to approval. All code contributed to this project will be licensed under the MIT License. If you would like to contribute in other ways, you're welcome to spread information about the API to friends and family or tip me via [PayPal](https://paypal.me/kdethlefs).