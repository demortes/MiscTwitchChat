using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiscTwitchChat.Classlib.Entities;
using MiscTwitchChat.Helpers;
using System;
using System.Threading.Tasks;

namespace MiscTwitchChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GiftController : ControllerBase
    {
        private readonly ILogger _logger;
        private readonly MiscTwitchDbContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="GiftController"/> using the provided logger factory and database context.
        /// </summary>
        public GiftController(ILoggerFactory logger, MiscTwitchDbContext context)
        {
            _logger = logger.CreateLogger<CardsController>();
            _context = context;
        }

        // GET: api/Cards
        /// <summary>
        /// Gifts a random item to a user in a channel.
        /// </summary>
        /// <param name="channel">The channel the gift is being given in.</param>
        /// <param name="fromUser">The user giving the gift.</param>
        /// <param name="giftingUsername">The user receiving the gift. If not specified, a random user will be chosen.</param>
        /// <summary>
        /// Selects or validates a recipient and returns a message describing a gift being handed to that recipient.
        /// </summary>
        /// <param name="channel">Twitch channel identifier where the gift is being given.</param>
        /// <param name="fromUser">The user who is giving the gift.</param>
        /// <param name="giftingUsername">Optional recipient username; if null or equal to <paramref name="fromUser"/>, a random consenting recipient is chosen. If no consenting recipient is found, a special message is returned.</param>
        /// <returns>
        /// A formatted message like "{fromUser} hands over a gift of {gift} to {giftingUsername}", or the string "No active consenting users found.... Sad Panda." when no recipient is available.
        /// </returns>
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
