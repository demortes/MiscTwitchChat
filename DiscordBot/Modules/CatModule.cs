using Discord;
using Discord.Interactions;
using DiscordBot.DemAPI;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class CatModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly Client _apiService;

        public CatModule(Client apiService)
        {
            _apiService = apiService;
        }

        [SlashCommand("cat", "Get a fact about one of the worlds favorite animals.")]
        public async Task Cat(IUser target = null)
        {
            var reply = await _apiService.ApiCatAsync();
            await RespondAsync(reply);
        }
    }
}
