using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class CatModule : InteractionModuleBase<SocketInteractionContext>
    {
        private IConfiguration _config;

        public CatModule(IConfiguration config)
        {
            _config = config;
        }

        [SlashCommand("cat", "Get a fact about one of the worlds favorite animals.")]
        public async Task Cat(IUser target = null)
        {
            var channel = Context.Channel.Name;
            var user = Context.User.Username;
            var url = _config.GetValue<string>("BaseAPIUrl");
            var apiService = new DemAPI.Client(url, new HttpClient());
            apiService.ReadResponseAsString = true;
            var reply = await apiService.ApiCatAsync();
            await RespondAsync(reply);
        }
    }
}
