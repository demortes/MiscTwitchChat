using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using UrbanDictionaryNet;

namespace MiscTwitchChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UrbanController : ControllerBase
    {
        private readonly ILogger _logger;

        public UrbanController(ILoggerFactory logger)
        {
            _logger = logger.CreateLogger<UrbanController>();
        }

        /// <summary>
        /// Gets the definition of a term from Urban Dictionary.
        /// </summary>
        /// <param name="term">The term to look up.</param>
        /// <returns>A string containing the definition of the term, truncated to approximately 255 characters.</returns>
        [HttpGet("{term}")]
        public string Get(string term)
        {
            var guid = Guid.NewGuid().ToString();
            var headers = HttpContext.Request.Headers;
            foreach (var header in headers)
            {
                _logger.LogInformation($"[{guid} Header: {header.Key} - Value: {header.Value}");
            }
            if (term.ToLower() == "demortes")
            {
                return "An awesome individual who doesn't need his own Urban Dictionary entry. Creator of this API. Nice try chump.";
            }
            else if (term.ToLower() == "LittleChrissie".ToLower())
            {
                return "LittleChrissie can't remove this either, but that's OK. They're pretty great.";
            }
            try
            {
                //Look it up.
                var result = UrbanDictionary.Define(term);

                if (result.Definitions.Count < 1)
                {
                    return "404: " + term + " not found.";
                }
                //Pass it back in formatted string, truncated.
                var fullResult = result.Definitions.OrderByDescending(p => p.CurrentVote).First().Definition.Replace("\r\n", " ").Replace("\r", " ").Replace("\n", " ");
                var truncatedResult = fullResult.Substring(0, fullResult.Length < 175 ? fullResult.Length : 175) + (fullResult.Length > 175 ? "..." : "");

                return truncatedResult;
            }
            catch
            {
                return "ABORT ABORT ABORT: Server error. Something broke, but not likely your bot.";
            }
        }
    }
}
