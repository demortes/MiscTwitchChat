using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MiscTwitchChat.Health
{
    /// <summary>
    /// Options for <see cref="ApiHealthCheck"/>.
    /// </summary>
    public sealed class ApiHealthCheckOptions
    {
        public Uri Url { get; set; }
    }

    /// <summary>
    /// Probes the API this service depends on. Reports unhealthy when the API is unreachable
    /// or answers with a non-success status.
    /// </summary>
    public sealed class ApiHealthCheck : IHealthCheck
    {
        private readonly HttpClient _httpClient;
        private readonly ApiHealthCheckOptions _options;

        public ApiHealthCheck(HttpClient httpClient, ApiHealthCheckOptions options)
        {
            _httpClient = httpClient;
            _options = options;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                using var response = await _httpClient.GetAsync(_options.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    return HealthCheckResult.Unhealthy(
                        $"{_options.Url} returned {(int)response.StatusCode} {response.ReasonPhrase}.");
                }

                return HealthCheckResult.Healthy($"{_options.Url} responded {(int)response.StatusCode}.");
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // HttpClient surfaces its own timeout as a cancellation.
                return HealthCheckResult.Unhealthy($"{_options.Url} timed out after {_httpClient.Timeout.TotalSeconds:0.#}s.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"{_options.Url} could not be reached.", ex);
            }
        }
    }
}
