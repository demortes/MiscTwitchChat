using Discord;
using Discord.Commands;
using Discord.Interactions;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class DadModule : InteractionModuleBase<SocketInteractionContext>
    {
        private IConfiguration _config;

        public DadModule(IConfiguration config)
        {
            _config = config;
        }

        [SlashCommand("dad", "Summon the inner dad and make a joke.")]
        public async Task Insult(IUser target = null)
        {
            var channel = Context.Channel.Name;
            var user = Context.User.Username;
            var url = _config.GetValue<string>("BaseAPIUrl");
            var apiService = new DemAPI.Client(url, new HttpClient());
            apiService.ReadResponseAsString = true;
            var reply = await apiService.ApiDadAsync();
            await RespondAsync(reply);
        }
    }
}
