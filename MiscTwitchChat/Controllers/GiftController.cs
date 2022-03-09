using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiscTwitchChat.Classlib.Entities;
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
        public async Task<string> Get(string channel, string fromUser, string giftingUsername = null)
        {
            if (string.IsNullOrEmpty(giftingUsername) || giftingUsername == fromUser)
            {
                giftingUsername = TwitchApiClasslib.GetRandomConsentingChatter(_context, channel, fromUser, "gift", false);
                if (string.IsNullOrEmpty(giftingUsername))
                {
                    return "No active consenting users found.... Sad Panda.";
                }
            }
            //Increase count on target user.
            var targetDbRecord = await _context.CommandCounts.FirstOrDefaultAsync(x => x.Channel == channel && x.TargetUser == fromUser && x.CommandUsed == "gift");
            if (targetDbRecord == null)
            {
                targetDbRecord = new CommandCount
                {
                    CommandUsed = "gift",
                    TargetUser = fromUser,
                    Channel = channel
                };
                _context.Add(targetDbRecord);
            }
            targetDbRecord.Count++;
            await _context.SaveChangesAsync();
            var gifts = System.IO.File.ReadAllLines("gifts.txt");

            var gift = gifts[new Random().Next(gifts.Length - 1)].ToString();
            return $"{fromUser} hands over a gift of {gift} to {giftingUsername}";
        }
    }
}
