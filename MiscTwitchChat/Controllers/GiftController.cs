using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MiscTwitchChat.Helpers;

namespace MiscTwitchChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GiftController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly MiscTwitchDbContext _context;

        public GiftController(ILoggerFactory logger, MiscTwitchDbContext context)
        {
            _logger = logger.CreateLogger<CardsController>();
            _context = context;
        }

        // GET: api/Cards
        [HttpGet]
        public string Get(string channel, string fromUser, string giftingUsername = null)
        {
            if (string.IsNullOrEmpty(giftingUsername))
            {
                var giftingUsernames = _context.ActiveChatters.Where(x => x.Channel == channel).ToArray();
                giftingUsername = giftingUsernames[new Random().Next(giftingUsername.Length - 1)].Username;
                if (string.IsNullOrEmpty(giftingUsername))
                {
                    return "No active consenting users found.... Sad Panda.";
                }
            }
            var gifts = System.IO.File.ReadAllLines("gifts.txt");

            var gift = gifts[new Random().Next(gifts.Length - 1)].ToString();
            return $"{fromUser} hands over a gift of {gift} to {giftingUsername}";
        }
    }
}
