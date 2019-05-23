using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;

namespace MiscTwitchChat
{
    public class ActionFilter : IActionFilter
    {
        private readonly ILogger _logger;

        public ActionFilter(ILoggerFactory factory)
        {
            _logger = factory.CreateLogger<ActionFilter>();
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            var guid = Guid.NewGuid().ToString();
            foreach (var header in context.HttpContext.Request.Headers)
            {
                _logger.LogInformation($"[{guid}]Header {header.Key} - {header.Value}");
            }
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            
        }
    }
}
