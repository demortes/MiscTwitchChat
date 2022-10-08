using Discord;
using Discord.Commands;
using Discord.Interactions;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class DogModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IConfiguration _config;

        public DogModule(IConfiguration config)
        {
            _config = config;
        }

        [SlashCommand("dog", "Facts about woofers.")]
#pragma warning disable IDE0060 // Remove unused parameter
        public async Task Dog(IUser target = null)
#pragma warning restore IDE0060 // Remove unused parameter
        {
            var url = _config.GetValue<string>("BaseAPIUrl");
            var apiService = new DemAPI.Client(url, new HttpClient())
            {
                ReadResponseAsString = true
            };
            var reply = await apiService.ApiDogAsync();
            await RespondAsync(reply);
        }
    }
}
