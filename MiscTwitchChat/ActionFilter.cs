using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Diagnostics;

namespace MiscTwitchChat
{
    public class ActionFilter : IActionFilter
    {
        private readonly ILogger<ActionFilter> _logger;
        private Stopwatch _stopwatch;
        private CfScope _cfScope;

        public ActionFilter(ILogger<ActionFilter> logger)
        {
            _logger = logger;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            _stopwatch = Stopwatch.StartNew();

            var headers = context.HttpContext.Request.Headers;

            // Extract Cloudflare headers and push as log properties so they
            // enrich the single request log line instead of generating noise.
            var cfRay          = headers["cf-ray"].ToString();
            var cfCountry      = headers["cf-ipcountry"].ToString();
            var cfConnectingIp = headers["cf-connecting-ip"].ToString();
            var cfVisitor      = headers["cf-visitor"].ToString();
            var forwardedFor   = headers["x-forwarded-for"].ToString();

            _cfScope = new CfScope(
                cfRay.Length          > 0 ? LogContext.PushProperty("CF-Ray", cfRay)                   : null,
                cfCountry.Length      > 0 ? LogContext.PushProperty("CF-Country", cfCountry)            : null,
                cfConnectingIp.Length > 0 ? LogContext.PushProperty("CF-ConnectingIP", cfConnectingIp)  : null,
                cfVisitor.Length      > 0 ? LogContext.PushProperty("CF-Visitor", cfVisitor)             : null,
                forwardedFor.Length   > 0 ? LogContext.PushProperty("X-Forwarded-For", forwardedFor)    : null
            );
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            _stopwatch.Stop();
            var request  = context.HttpContext.Request;
            var response = context.HttpContext.Response;

            using (LogContext.PushProperty("RemoteIP", context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"))
            using (LogContext.PushProperty("UserAgent", request.Headers.UserAgent.ToString()))
            using (LogContext.PushProperty("ElapsedMs", _stopwatch.ElapsedMilliseconds))
            {
                _logger.LogInformation(
                    "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                    request.Method, request.Path, response.StatusCode, _stopwatch.ElapsedMilliseconds);
            }

            _cfScope?.Dispose();
        }

        private sealed class CfScope(params IDisposable[] handles) : IDisposable
        {
            public void Dispose() { foreach (var h in handles) h?.Dispose(); }
        }
    }
}
