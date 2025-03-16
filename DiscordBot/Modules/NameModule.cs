using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class NameModule : InteractionModuleBase<SocketInteractionContext>
    {
        private IConfiguration _config;

        public NameModule(IConfiguration config)
        {
            _config = config;
        }

        [SlashCommand("generate-name", "Generate a name to be used in a video game. Pick the game, and the object you want a name for.")]
        public async Task GenerateName(string game, string item)
        {
            // To be finished once API is published.
            var channel = Context.Channel.Name;
            var user = Context.User.Username;
            var url = _config.GetValue<string>("BaseAPIUrl");
            var apiService = new DemAPI.Client(url, new HttpClient())
            {
                ReadResponseAsString = true
            };
            var reply = await apiService.ApiAiNameAsync(game, item);
            await RespondAsync(reply);
        }
    }
}
