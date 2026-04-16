using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MiscTwitchChat.Classlib.Entities;
using MiscTwitchChat.Helpers;
using System.Linq;
using System.Threading.Tasks;

namespace MiscTwitchChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HugController : ControllerBase
    {
        private readonly MiscTwitchDbContext _db;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="HugController"/> with the provided logger and database context.
        /// </summary>
        public HugController(ILogger<SpankController> logger, MiscTwitchDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        /// <summary>
        /// Hugs a random consenting user in a channel.
        /// </summary>
        /// <param name="channel">The channel the hug is being given in.</param>
        /// <param name="origUser">The user giving the hug.</param>
        /// <summary>
        /// Processes a hug request from a user in a channel, updates the command usage count, and returns a status message.
        /// </summary>
        /// <param name="channel">The channel where the hug is initiated.</param>
        /// <param name="origUser">The user who initiated the hug.</param>
        /// <returns>
        /// A message stating either that the initiating user has registered non-consent and cannot hug,
        /// or confirming which target was hugged by the initiating user (and that the hug was recorded).
        /// </returns>
        [HttpGet("{channel}/{origUser}")]
        public async Task<string> HugAsync(string channel, string origUser)
        {
            _logger.LogInformation($"Starting Hug from {origUser} in {channel}");
            if (_db.Disconsenters.FirstOrDefault(p => p.Name == origUser) != null)
            {
                return $"{origUser} does not consent and so is not allowed to hug.";
            }

            string target = TwitchApiClasslib.GetRandomConsentingChatter(_db, channel, origUser, "hug", false);

            //Increase count on target user.
            var targetDbRecord = await _db.CommandCounts.FirstOrDefaultAsync(x => x.Channel == channel && x.TargetUser == target && x.CommandUsed == "hug");
            if (targetDbRecord == null)
            {
                targetDbRecord = new CommandCount
                {
                    CommandUsed = "hug",
                    TargetUser = target,
                    Channel = channel
                };
                _db.Add(targetDbRecord);
            }
            targetDbRecord.Count++;
            await _db.SaveChangesAsync();

            return $"Well well well, {target} was hugged by {origUser}. WAS THERE EVEN CONSENT?!";
        }

        /// <summary>
        /// Updates the consent status for a user.
        /// </summary>
        /// <param name="channel">The channel the user is in.</param>
        /// <param name="origUser">The user to update consent for.</param>
        /// <summary>
        /// Toggles the stored consent status for the specified user.
        /// </summary>
        /// <param name="channel">The channel name (accepted but not used by this endpoint).</param>
        /// <param name="origUser">The user whose consent status will be registered or removed.</param>
        /// <returns>A message stating the user's new consent status.</returns>
        [HttpGet("{channel}/{origUser}/consent")]
        public async Task<string> UpdateConsent(string channel, string origUser)
        {
            string rval = string.Empty;
            var disconsenter = _db.Disconsenters.FirstOrDefault(p => p.Name == origUser);
            if (disconsenter == null)
            {
                _db.Disconsenters.Add(new Disconsenter
                {
                    Name = origUser
                });
                rval = $"{origUser} has registered they do not consent.";
            }
            else
            {
                _db.Disconsenters.Remove(disconsenter);
                rval = $"{origUser} has registered that they DO consent.";
            }

            await _db.SaveChangesAsync();
            return rval;
        }
    }
}