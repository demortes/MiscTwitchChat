using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace MiscTwitchChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CardsController : ControllerBase
    {
        private readonly ILogger _logger;

        public CardsController(ILogger logger)
        {
            _logger = logger;
        }

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
            var blackCards = System.IO.File.ReadAllLines("black_cards.txt");
            var whiteCards = System.IO.File.ReadAllLines("white_cards.txt");

            var blackCard = blackCards[new Random().Next(0, blackCards.Length - 1)];
            var whiteCard = whiteCards[new Random().Next(0, whiteCards.Length - 1)];

            var returnString = $"Prompt: {blackCard}\r\nReponse: {whiteCard}";
            return returnString;
        }

    }
}
