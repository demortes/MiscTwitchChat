using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Linq;

namespace MiscTwitchChat.Health
{
    public static class HealthChecksExtensions
    {
        /// <summary>
        /// Registers a check that the API this service depends on is reachable.
        /// </summary>
        public static IHealthChecksBuilder AddApiCheck(
            this IHealthChecksBuilder builder,
            string name,
            Uri url,
            TimeSpan timeout,
            params string[] tags)
        {
            builder.Services.AddSingleton(new ApiHealthCheckOptions { Url = url });
            builder.Services.AddHttpClient<ApiHealthCheck>(client => client.Timeout = timeout);
            return builder.AddCheck<ApiHealthCheck>(name, HealthStatus.Unhealthy, tags);
        }

        /// <summary>
        /// Registers a check that the database behind <typeparamref name="TContext"/> is reachable.
        /// </summary>
        public static IHealthChecksBuilder AddDbContextConnectionCheck<TContext>(
            this IHealthChecksBuilder builder,
            string name,
            params string[] tags) where TContext : DbContext
        {
            return builder.AddCheck<DbContextHealthCheck<TContext>>(name, HealthStatus.Unhealthy, tags);
        }

        /// <summary>
        /// Registers a check over a long lived client connection the service owns.
        /// </summary>
        public static IHealthChecksBuilder AddConnectionCheck(
            this IHealthChecksBuilder builder,
            string name,
            Func<bool> isConnected,
            string connectedDescription,
            string disconnectedDescription,
            params string[] tags)
        {
            var check = new ConnectionHealthCheck(isConnected, connectedDescription, disconnectedDescription);
            return builder.AddCheck(name, check, HealthStatus.Unhealthy, tags);
        }

        /// <summary>
        /// Maps the three health endpoints used by every service in this solution:
        /// <c>/health/live</c> (process is answering, no dependencies probed),
        /// <c>/health/ready</c> (every dependency this service needs), and
        /// <c>/health</c> (everything registered).
        /// </summary>
        /// <remarks>
        /// These are terminal middleware branches rather than routed endpoints, so they work in the API
        /// (which still uses <c>UseMvc</c> with endpoint routing disabled) and in the bots' minimal hosts alike.
        /// Register this early in the pipeline - ahead of HTTPS redirection - so a plain HTTP probe from the
        /// Docker health check is answered instead of being handed a 307 that curl would read as success.
        /// The specific paths must be mapped before the catch-all <c>/health</c>.
        /// </remarks>
        public static IApplicationBuilder UseHealthEndpoints(this IApplicationBuilder app)
        {
            app.UseHealthChecks(HealthEndpoint.LivePath, new HealthCheckOptions
            {
                Predicate = _ => false,
                ResponseWriter = HealthReportWriter.Write
            });

            app.UseHealthChecks(HealthEndpoint.ReadyPath, new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains(HealthEndpoint.ReadyTag),
                ResponseWriter = HealthReportWriter.Write
            });

            app.UseHealthChecks(HealthEndpoint.RootPath, new HealthCheckOptions
            {
                ResponseWriter = HealthReportWriter.Write
            });

            return app;
        }
    }
}
