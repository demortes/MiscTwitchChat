﻿using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using UrbanDictionaryNet;

namespace MiscTwitchChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UrbanController : ControllerBase
    {
        /// <summary>
        /// Get the term from Urban Dictionary. Truncated to 255 ish characters, and single line.
        /// </summary>
        /// <param name="term">Term to look for.</param>
        /// <returns>String of 255 ish characters, used for the definition.</returns>
        [HttpGet("{term}")]
        public string Get(string term)
        {
            try
            {
                //Look it up.
                var result = UrbanDictionary.Define(term);

                if (result.Definitions.Count < 1)
                {
                    return "404: " + term + " not found.";
                }
                //Pass it back in formatted string, truncated.
                var fullResult = result.Definitions.OrderByDescending(p => p.CurrentVote).First().Definition.Replace("\r\n", " ");
                var truncatedResult = fullResult.Substring(0, fullResult.Length < 252 ? fullResult.Length : 252) + (fullResult.Length > 252 ? "..." : "");

                return truncatedResult;
            }
            catch
            {
                return "ABORT ABORT ABORT: Server error. Something broke, but not likely your bot.";
            }
        }
    }
}
