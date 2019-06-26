using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MiscTwitchChat.Controllers
{
    /// <summary>
    /// Get your eight ball here.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class EightBallController : ControllerBase
    {
        public EightBallController(ILoggerFactory logger)
        {
            _logger = logger.CreateLogger<EightBallController>();
        }
        private string[] response =
        {
            "Yes, of course!",
            "Without a doubt, yes.",
            "You can count on it.",
            "For sure!",
            "Ask me later.",
            "I'm not sure.",
            "I can't tell you right now.",
            "I'll tell you after my nap.",
            "No way!",
            "Doesn't look great.",
            "I don't think so.",
            "Without a doubt, no.",
            "The answer is clearly NO!",
            "Today doesn't look good. Tomorrow doesn't either. Nor the year for that fact.",
            "Only I will ever know",
            "I better not say.",
            "Repeat after me... I am not that lucky.", 
            "Only if your name is Zach.",
            "What are you? Stupid?",
            "What the hell kind of question is that?",
            "HAH! In your dreams.",
            "Sure, why not?",
            "Ra made me say yes.",
            "Only if you eat smegma.",
            "Help me.... The oompa loompa uses me when he decides to go to war..."
        };
        private readonly ILogger _logger;

        [HttpGet]
        public string Get()
        {
            var r = response[new Random().Next(0, response.Length - 1)];
            var guid = Guid.NewGuid().ToString();
            var headers = HttpContext.Request.Headers;
            foreach (var header in headers)
            {
                _logger.LogInformation($"[{guid} Header: {header.Key} - Value: {header.Value}");
            }

            return r;
        }
    }
}
