using Discord;
using Discord.Commands;
using Discord.Interactions;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class InsultModule : InteractionModuleBase<SocketInteractionContext>
    {
        private IConfiguration _config;

        public InsultModule(IConfiguration config)
        {
            _config = config;
        }

        [SlashCommand("insult", "Insult someone. Either tag them or let the bot pick one randomly.")]
        public async Task Insult(IUser target = null)
        {
            var channel = Context.Channel.Name;
            var user = Context.User.Username;
            var url = _config.GetValue<string>("BaseAPIUrl");
            var apiService = new DemAPI.Client(url, new HttpClient());
            apiService.ReadResponseAsString = true;
            var reply = await apiService.ApiInsultAsync(channel, user, target?.Username);
            await RespondAsync(reply);
        }
    }
}
