using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiscTwitchChat.Health
{
    /// <summary>
    /// Opens a connection to the database behind <typeparamref name="TContext"/>. This is a real round trip
    /// to the server, not just a check that a connection string was configured.
    /// </summary>
    public sealed class DbContextHealthCheck<TContext> : IHealthCheck where TContext : DbContext
    {
        private readonly IServiceProvider _services;

        public DbContextHealthCheck(IServiceProvider services)
        {
            _services = services;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                // Resolved here rather than injected: configuring the context can itself hit the database
                // (Pomelo's ServerVersion.AutoDetect does), and a constructor throwing inside the health
                // check factory escapes as a 500 instead of being reported as unhealthy.
                var dbContext = _services.GetRequiredService<TContext>();

                if (await dbContext.Database.CanConnectAsync(cancellationToken))
                {
                    return HealthCheckResult.Healthy($"{typeof(TContext).Name} connected to the database.");
                }

                return HealthCheckResult.Unhealthy($"{typeof(TContext).Name} could not connect to the database.");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"{typeof(TContext).Name} failed to connect to the database.", ex);
            }
        }
    }
}
