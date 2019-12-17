using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MiscTwitchChat.Helpers;

namespace MiscTwitchChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CardsController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly CAH_cards cahCards;

        public CardsController(ILoggerFactory logger, CAH_cards cards)
        {
            _logger = logger.CreateLogger<CardsController>();
            cahCards = cards;
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

        // GET: api/Cards
        [HttpGet]
        public string Get()
        {
            var guid = Guid.NewGuid().ToString();
            var headers = HttpContext.Request.Headers;
            foreach (var header in headers)
            {
                _logger.LogInformation($"[{guid} Header: {header.Key} - Value: {header.Value}");
            }

            var rval = @"";
            var blackCard = cahCards.blackCards[new Random().Next(0, cahCards.blackCards.Length)];
            rval += StripHTML(blackCard.text);
            for(int y = 0; y < blackCard.pick;y++)
            {
                var whiteCard = cahCards.whiteCards[new Random().Next(0, cahCards.whiteCards.Length)];
                if (rval.Contains('_'))
                    rval = rval.ReplaceFirst("_", replace: $"_{StripHTML(whiteCard)}_");
                else
                    rval += $"_{StripHTML(whiteCard)}_";
            }
            return rval;
        }

        public static string StripHTML(string input)
        {
            return Regex.Replace(input, "<.*?>", "");
        }

    }
    public static class StringExtensionMethods
    {
        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }
    }
}
