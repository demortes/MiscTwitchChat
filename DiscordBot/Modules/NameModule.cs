using Discord.Interactions;
using DiscordBot.DemAPI;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class NameModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly Client _apiService;

        public NameModule(Client apiService)
        {
            _apiService = apiService;
        }

        [SlashCommand("generate-name", "Generate a name to be used in a video game. Pick the game, and the object you want a name for.")]
        public async Task GenerateName(string game, string item)
        {
            var reply = await _apiService.ApiAiNameAsync(game, item);
            await RespondAsync(reply);
        }
    }
}
