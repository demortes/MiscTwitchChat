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

        public SpankController(ILogger<SpankController> logger, MiscTwitchDbContext db)
        {
            _logger = logger;
            _db = db;
        }

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