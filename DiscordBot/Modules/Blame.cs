using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class BlameModule : InteractionModuleBase<SocketInteractionContext>
    {
        private IConfiguration _config;

        public BlameModule(IConfiguration config)
        {
            _config = config;
        }

        [SlashCommand("blame", "Blame someone.... anyone...")]
        public async Task Blame(IUser target)
        {
            var user = Context.User.Username;
            var reply = $"We should all blame {target}";
            await RespondAsync(reply);
        }

        [SlashCommand("blamedem", "Demortes has his own blame command...")]
        public async Task BlameDem()
        {
            var user = Context.User.Username;
            var reply = $"Demortes is at it again... WTF Demortes.";
            await RespondAsync(reply);
        }

        [SlashCommand("blamecody", "Australians get one too....")]
        public async Task BlameCody()
        {
            var user = Context.User.Username;
            var reply = $"U wot m8? Cody's wheezing again....";
            await RespondAsync(reply);
        }
    }
}
