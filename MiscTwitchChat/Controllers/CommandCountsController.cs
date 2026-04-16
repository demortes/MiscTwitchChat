using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace MiscTwitchChat.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CommandCountsController : ControllerBase
    {
        private readonly MiscTwitchDbContext _context;

        public CommandCountsController(MiscTwitchDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the number of times a command has been used on a target user in a specific channel.
        /// </summary>
        /// <param name="channel">The channel the command was used in.</param>
        /// <param name="targetUser">The user the command was used on.</param>
        /// <param name="commandUsed">The command that was used.</param>
        /// <returns>The number of times the command has been used.</returns>
        [HttpGet("{channel}/{targetUser}/{commandUsed}")]
        public async Task<int> GetAsync(string channel, string targetUser, string commandUsed)
        {
            var targetData = await _context.CommandCounts.FirstOrDefaultAsync(x => x.Channel == channel && x.TargetUser == targetUser && x.CommandUsed == commandUsed);
            if (targetData == null)
            {
                return 0;
            }

            return targetData.Count;
        }
    }
}
