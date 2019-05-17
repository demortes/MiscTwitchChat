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
            _logger.LogInformation($"Headers: {context.HttpContext.Request.Headers}");
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            
        }
    }
}
