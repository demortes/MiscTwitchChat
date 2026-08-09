using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;

namespace MiscTwitchChat.Health
{
    /// <summary>
    /// Hosts the health endpoints for the background services, which have no web host of their own.
    /// </summary>
    public static class HealthEndpoint
    {
        public const string RootPath = "/health";
        public const string LivePath = "/health/live";
        public const string ReadyPath = "/health/ready";

        /// <summary>Checks tagged with this are dependencies the service cannot work without.</summary>
        public const string ReadyTag = "ready";

        private const int DefaultPort = 8080;
        private const string DefaultApiHealthPath = "health/live";
        private const double DefaultApiTimeoutSeconds = 5;

        /// <summary>
        /// Starts the health listener on <c>Health:Port</c> (default 8080) and returns without blocking.
        /// Dispose the returned host to shut it down.
        /// </summary>
        /// <param name="configuration">Configuration the calling service already built.</param>
        /// <param name="configureChecks">Registers the checks this service should report on.</param>
        public static WebApplication Start(IConfiguration configuration, Action<IHealthChecksBuilder> configureChecks)
        {
            var port = configuration.GetValue<int?>("Health:Port") ?? DefaultPort;

            var builder = WebApplication.CreateSlimBuilder();

            // The bots configure Serilog before starting this, so route the host's own logs through it.
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog(Log.Logger, dispose: false);

            builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

            configureChecks(builder.Services.AddHealthChecks());

            var app = builder.Build();
            app.UseHealthEndpoints();
            app.Start();

            Log.Information("Health endpoints listening on http://0.0.0.0:{Port}{Path}", port, RootPath);

            return app;
        }

        /// <summary>
        /// Builds the URL of the API's health endpoint from <c>BaseAPIUrl</c> and the optional
        /// <c>Health:ApiHealthPath</c>. Returns null when no API is configured, so the caller can skip the check.
        /// </summary>
        public static Uri ResolveApiHealthUri(IConfiguration configuration)
        {
            var baseUrl = configuration.GetValue<string>("BaseAPIUrl");
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return null;
            }

            if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
            {
                baseUrl += "/";
            }

            var path = configuration.GetValue<string>("Health:ApiHealthPath");
            if (string.IsNullOrWhiteSpace(path))
            {
                path = DefaultApiHealthPath;
            }

            return new Uri(new Uri(baseUrl, UriKind.Absolute), path.TrimStart('/'));
        }

        /// <summary>
        /// How long to wait on the API before calling it unreachable, from <c>Health:ApiTimeoutSeconds</c>.
        /// </summary>
        public static TimeSpan ResolveApiTimeout(IConfiguration configuration)
        {
            return TimeSpan.FromSeconds(configuration.GetValue<double?>("Health:ApiTimeoutSeconds") ?? DefaultApiTimeoutSeconds);
        }
    }
}
