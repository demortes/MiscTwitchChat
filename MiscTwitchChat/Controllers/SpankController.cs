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
    public class SpankController : ControllerBase
    {
        private readonly MiscTwitchDbContext _db;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="SpankController"/> and stores the provided logger and database context.
        /// </summary>
        public SpankController(ILogger<SpankController> logger, MiscTwitchDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        /// <summary>
        /// Spanks a random consenting user in a channel.
        /// </summary>
        /// <param name="channel">The channel the spank is being given in.</param>
        /// <param name="origUser">The user giving the spank.</param>
        /// <summary>
        /// Chooses a random consenting chatter in the specified channel, records a "spank" command usage for the chosen target, and returns a confirmation message.
        /// </summary>
        /// <param name="channel">The channel in which the spank is initiated.</param>
        /// <param name="origUser">The user initiating the spank; if this user is registered as not consenting, the operation is blocked.</param>
        /// <returns>A message confirming who was spanked, or a message stating the initiator does not consent and cannot spank.</returns>
        [HttpGet("{channel}/{origUser}")]
        public async Task<string> SpankAsync(string channel, string origUser)
        {
            _logger.LogInformation($"Starting spank from {origUser} in {channel}");
            if (_db.Disconsenters.FirstOrDefault(p => p.Name == origUser) != null)
            {
                return $"{origUser} does not consent and so is not allowed to spank.";
            }
            string target = TwitchApiClasslib.GetRandomConsentingChatter(_db, channel, origUser, "spank", false);

            //Increase count on target user.
            var targetDbRecord = await _db.CommandCounts.FirstOrDefaultAsync(x => x.Channel == channel && x.TargetUser == target && x.CommandUsed == "spank");
            if (targetDbRecord == null)
            {
                targetDbRecord = new CommandCount
                {
                    CommandUsed = "spank",
                    TargetUser = target,
                    Channel = channel
                };
                _db.Add(targetDbRecord);
            }
            targetDbRecord.Count++;
            await _db.SaveChangesAsync();

            return $"Well well well, {target} was spanked by {origUser}. WAS THERE EVEN CONSENT?!";
        }

        /// <summary>
        /// Updates the consent status for a user.
        /// </summary>
        /// <param name="channel">The channel the user is in.</param>
        /// <param name="origUser">The user to update consent for.</param>
        /// <summary>
        /// Toggles the consent state for the specified user in the database and returns a status message.
        /// </summary>
        /// <param name="channel">Channel name from the route (accepted but not used in the database update).</param>
        /// <param name="origUser">The username whose consent status will be toggled.</param>
        /// <returns>A message stating that the user has registered non-consent ("&lt;user&gt; has registered they do not consent.") or that the user has registered consent ("&lt;user&gt; has registered that they DO consent.").</returns>
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