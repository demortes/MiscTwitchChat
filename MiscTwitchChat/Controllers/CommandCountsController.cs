using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

        [HttpGet("{channel}/{targetUsers}/{commandUsed}")]
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
