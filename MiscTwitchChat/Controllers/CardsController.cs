using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MiscTwitchChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CardsController : ControllerBase
    {

        // GET: api/Cards
        [HttpGet]
        public string Get()
        {
            var blackCards = System.IO.File.ReadAllLines("black_cards.txt");
            var whiteCards = System.IO.File.ReadAllLines("white_cards.txt");

            var blackCard = blackCards[new Random().Next(0, blackCards.Length - 1)];
            var whiteCard = whiteCards[new Random().Next(0, whiteCards.Length - 1)];

            var returnString = $"Prompt: {blackCard}\r\nReponse: {whiteCard}";
            return returnString;
        }

    }
}
