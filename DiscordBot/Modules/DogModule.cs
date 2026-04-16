using Discord.Interactions;
using DiscordBot.DemAPI;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class DogModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly Client _apiService;

        public DogModule(Client apiService)
        {
            _apiService = apiService;
        }

        [SlashCommand("dog", "Facts about woofers.")]
        public async Task Dog()
        {
            var reply = await _apiService.ApiDogAsync();
            await RespondAsync(reply);
        }
    }
}
