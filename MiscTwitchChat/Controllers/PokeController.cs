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
    public class PokeController : ControllerBase
    {
        private readonly MiscTwitchDbContext _db;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="PokeController"/> using the provided logger and database context.
        /// </summary>
        public PokeController(ILogger<SpankController> logger, MiscTwitchDbContext db)
        {
            _logger = logger;
            _db = db;
        }

        /// <summary>
        /// Pokes a random consenting user in a channel.
        /// </summary>
        /// <param name="channel">The channel the poke is being given in.</param>
        /// <param name="origUser">The user giving the poke.</param>
        /// <summary>
        /// Pokes a random consenting user in the specified channel on behalf of the originating user.
        /// </summary>
        /// <param name="channel">The channel in which to select a consenting chatter.</param>
        /// <param name="origUser">The user initiating the poke.</param>
        /// <returns>A message stating either that <paramref name="origUser"/> does not consent and cannot poke, or that a selected target was poked by <paramref name="origUser"/>.</returns>
        [HttpGet("{channel}/{origUser}")]
        public async Task<string> HugAsync(string channel, string origUser)
        {
            _logger.LogInformation($"Starting poke from {origUser} in {channel}");
            if (_db.Disconsenters.FirstOrDefault(p => p.Name == origUser) != null)
            {
                return $"{origUser} does not consent and so is not allowed to poke.";
            }
            string target = TwitchApiClasslib.GetRandomConsentingChatter(_db, channel, origUser, "poke", false);

            //Increase count on target user.
            var targetDbRecord = await _db.CommandCounts.FirstOrDefaultAsync(x => x.Channel == channel && x.TargetUser == target && x.CommandUsed == "poke");
            if (targetDbRecord == null)
            {
                targetDbRecord = new CommandCount
                {
                    CommandUsed = "poke",
                    TargetUser = target,
                    Channel = channel
                };
                _db.Add(targetDbRecord);
            }
            targetDbRecord.Count++;
            await _db.SaveChangesAsync();

            return $"{target} was poke by {origUser}. POKE HARDER!";
        }

        /// <summary>
        /// Updates the consent status for a user.
        /// </summary>
        /// <param name="channel">The channel the user is in.</param>
        /// <param name="origUser">The user to update consent for.</param>
        /// <summary>
        /// Toggles the stored consent status for a user in the Disconsenters table.
        /// </summary>
        /// <param name="channel">The channel name from the route (routing/contextual identifier).</param>
        /// <param name="origUser">The user whose consent status will be toggled.</param>
        /// <returns>A message indicating the user's new consent status.</returns>
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