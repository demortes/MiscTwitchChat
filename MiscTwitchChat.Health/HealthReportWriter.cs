using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MiscTwitchChat.Health
{
    /// <summary>
    /// Writes a health report as JSON so a failing probe says which dependency broke, not just that something did.
    /// </summary>
    /// <remarks>
    /// Only descriptions written by this solution's own checks are returned. Exception detail is deliberately
    /// left out: these endpoints are unauthenticated, and a driver message can carry the database user, host
    /// or other connection detail. The health check infrastructure already logs the full exception at Error
    /// level (<c>HealthCheckEnd</c>, from <c>DefaultHealthCheckService</c>), so nothing is lost for an operator.
    /// </remarks>
    public static class HealthReportWriter
    {
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        public static Task Write(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            var payload = new
            {
                status = report.Status.ToString(),
                totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
                checks = report.Entries.Select(entry => new
                {
                    name = entry.Key,
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 1)
                }).ToArray()
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(payload, SerializerOptions));
        }
    }
}
