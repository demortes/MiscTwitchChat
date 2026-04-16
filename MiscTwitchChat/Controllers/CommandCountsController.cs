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

        /// <summary>
        /// Initializes a new instance of <see cref="CommandCountsController"/> with the specified database context.
        /// </summary>
        /// <param name="context">The <see cref="MiscTwitchDbContext"/> used to query and manage command count records.</param>
        public CommandCountsController(MiscTwitchDbContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// Gets how many times a specific command was used against a target user in a channel.
        /// </summary>
        /// <param name="channel">The channel identifier where the command usage was recorded.</param>
        /// <param name="targetUser">The username of the target who received the command.</param>
        /// <param name="commandUsed">The command string to count.</param>
        /// <returns>The count of times the specified command was used against the specified target in the given channel; 0 if no record exists.</returns>
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
