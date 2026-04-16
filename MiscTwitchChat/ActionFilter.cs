using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace MiscTwitchChat
{
    public class ActionFilter : IActionFilter
    {
        private readonly ILogger<ActionFilter> _logger;
        private Stopwatch _stopwatch;

        public ActionFilter(ILogger<ActionFilter> logger)
        {
            _logger = logger;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            _stopwatch = Stopwatch.StartNew();
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            _stopwatch.Stop();
            var request = context.HttpContext.Request;
            var response = context.HttpContext.Response;
            var remoteIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = request.Headers.UserAgent.ToString();

            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms from {RemoteIP} via {UserAgent}",
                request.Method,
                request.Path,
                response.StatusCode,
                _stopwatch.ElapsedMilliseconds,
                remoteIp,
                userAgent);
        }
    }
}
