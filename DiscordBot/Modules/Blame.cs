using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class BlameModule : ModuleBase<SocketCommandContext>
    {
        private IConfiguration _config;

        public BlameModule(IConfiguration config)
        {
            _config = config;
        }

        [Command("blame")]
        public async Task Blame(IUser target)
        {
            var user = Context.User.Username;
            var reply = $"We should all blame {target}";
            await ReplyAsync(reply);
        }

        [Command("blamedem")]
        public async Task BlameDem()
        {
            var user = Context.User.Username;
            var reply = $"Demortes is at it again... WTF Demortes.";
            await ReplyAsync(reply);
        }

        [Command("blamecody")]
        public async Task BlameCody()
        {
            var user = Context.User.Username;
            var reply = $"U wot m8? Cody's wheezing again....";
            await ReplyAsync(reply);
        }
    }
}
