using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MiscTwitchChat.Health
{
    /// <summary>
    /// Reports on a long lived client connection (Discord gateway, Twitch IRC) that the service owns.
    /// A bot that is up but disconnected is doing nothing useful, so it should not read as healthy.
    /// </summary>
    public sealed class ConnectionHealthCheck : IHealthCheck
    {
        private readonly Func<bool> _isConnected;
        private readonly string _connectedDescription;
        private readonly string _disconnectedDescription;

        public ConnectionHealthCheck(Func<bool> isConnected, string connectedDescription, string disconnectedDescription)
        {
            _isConnected = isConnected;
            _connectedDescription = connectedDescription;
            _disconnectedDescription = disconnectedDescription;
        }

        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                return Task.FromResult(_isConnected()
                    ? HealthCheckResult.Healthy(_connectedDescription)
                    : HealthCheckResult.Unhealthy(_disconnectedDescription));
            }
            catch (Exception ex)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(_disconnectedDescription, ex));
            }
        }
    }
}
