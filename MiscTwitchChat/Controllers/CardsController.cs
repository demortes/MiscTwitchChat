using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MiscTwitchChat.Helpers;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace MiscTwitchChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CardsController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly CAH_cards cahCards;
        private readonly MiscTwitchDbContext _context;
        private const string BanSettingName = "BANCAH";

        public CardsController(ILoggerFactory logger, CAH_cards cards, MiscTwitchDbContext context)
        {
            _logger = logger.CreateLogger<CardsController>();
            cahCards = cards;
            _context = context;
        }

        /// <summary>
        /// Bans the CAH command for a specified channel.
        /// </summary>
        /// <param name="channel">The channel to ban the command in.</param>
        /// <returns>A string indicating the result of the ban.</returns>
        [HttpGet("ban/{channel}")]
        public string BanCahGet(string channel)
        {
            return BanCah(channel);
        }

        /// <summary>
        /// Bans the CAH command for a specified channel.
        /// </summary>
        /// <param name="channel">The channel to ban the command in.</param>
        /// <returns>A string indicating the result of the ban.</returns>
        [HttpDelete("{channel}")]
        public string BanCah(string channel)
        {
            string rval = "Something went wrong. Please DM Demortes";
            try
            {
                //Get last ban.
                var banCah = _context.Settings.Where(x => x.Channel == channel && x.Name == BanSettingName).FirstOrDefault();
                if (banCah == null || DateTimeOffset.Parse(banCah.Value).AddMinutes(3) < DateTimeOffset.UtcNow)
                {
                    rval = $"CAH has been silenced for a while. Let's hope the punishment fit the crime.";
                    if (banCah == null)
                    {
                        banCah = new Classlib.Entities.Setting()
                        {
                            Channel = channel,
                            Name = BanSettingName
                        };

                        _context.Add(banCah);
                    }
                    banCah.Value = DateTimeOffset.UtcNow.AddMinutes(5).ToString();
                    _context.SaveChanges();
                }
                else if (DateTimeOffset.Parse(banCah.Value).AddMinutes(3) > DateTimeOffset.UtcNow)
                {
                    rval = "CAH was banned too recently. Give peace a chance.";
                }
                else
                {
                    rval = $"CAH is already banned. Let the punishment lapse first.";
                }
            }
            catch
            {

            }

            return rval;
        }
        //// GET: api/Cards
        //[HttpGet]
        //public string Get()
        //{
        //    var guid = Guid.NewGuid().ToString();
        //    var headers = HttpContext.Request.Headers;
        //    foreach (var header in headers)
        //    {
        //        _logger.LogInformation($"[{guid} Header: {header.Key} - Value: {header.Value}");
        //    }
        //    var blackCards = System.IO.File.ReadAllLines("black_cards.txt");
        //    var whiteCards = System.IO.File.ReadAllLines("white_cards.txt");

        //    var blackCard = blackCards[new Random().Next(0, blackCards.Length - 1)];
        //    var whiteCard = whiteCards[new Random().Next(0, whiteCards.Length - 1)];

        //    var returnString = $"Prompt: {blackCard}\r\nReponse: {whiteCard}";
        //    return returnString;
        //}

        /// <summary>
        /// Gets a random CAH card combination.
        /// </summary>
        /// <returns>A string containing a random CAH card combination.</returns>
        [HttpGet]
        public string Get()
        {
            return Get(null);
        }

        // GET: api/Cards
        /// <summary>
        /// Gets a random CAH card combination, optionally checking if the command is banned in the specified channel.
        /// </summary>
        /// <param name="channel">The channel to check for a ban.</param>
        /// <returns>A string containing a random CAH card combination.</returns>
        [HttpGet("{channel}")]
        public string Get(string channel = null)
        {
            var guid = Guid.NewGuid().ToString();
            var headers = HttpContext.Request.Headers;
            foreach (var header in headers)
            {
                _logger.LogInformation($"[{guid} Header: {header.Key} - Value: {header.Value}");
            }

            if (channel != null)
            {
                //get Channel setting.
                var banCah = _context.Settings.FirstOrDefault(x => x.Channel == channel && x.Name == BanSettingName);
                if (banCah != null && DateTimeOffset.Parse(banCah.Value) > DateTimeOffset.UtcNow)
                    return $"As CAH opens its mouth to impart words of callous wisdom, a magical force press it shut. Someone has been a bad algorithm.";
            }

            var rval = @"";
            //Get any 
            var blackCard = cahCards.blackCards[new Random().Next(0, cahCards.blackCards.Length)];
            rval += StripHTML(blackCard.text);
            for (int y = 0; y < blackCard.pick; y++)
            {
                var whiteCard = cahCards.whiteCards[new Random().Next(0, cahCards.whiteCards.Length)];
                if (rval.Contains('_'))
                    rval = rval.ReplaceFirst("_", replace: $"*{StripHTML(whiteCard)}*");
                else
                    rval += $"*{StripHTML(whiteCard)}*";
            }
            return rval;
        }

        /// <summary>
        /// Strips HTML tags from a string.
        /// </summary>
        /// <param name="input">The string to strip HTML from.</param>
        /// <returns>The string with HTML tags removed.</returns>
        public static string StripHTML(string input)
        {
            input = HttpUtility.HtmlDecode(input);
            return Regex.Replace(input, "<.*?>", " ");
        }

    }
    public static class StringExtensionMethods
    {
        /// <summary>
        /// Replaces the first occurrence of a string with another string.
        /// </summary>
        /// <param name="text">The string to modify.</param>
        /// <param name="search">The string to search for.</param>
        /// <param name="replace">The string to replace the first occurrence with.</param>
        /// <returns>The modified string.</returns>
        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text[(pos + search.Length)..];
        }
    }
}
