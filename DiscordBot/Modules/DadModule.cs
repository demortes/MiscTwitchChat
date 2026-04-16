using Discord.Interactions;
using DiscordBot.DemAPI;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class DadModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly Client _apiService;

        public DadModule(Client apiService)
        {
            _apiService = apiService;
        }

        [SlashCommand("dad", "Summon the inner dad and make a joke.")]
        public async Task Insult()
        {
            var reply = await _apiService.ApiDadAsync();
            await RespondAsync(reply);
        }
    }
}
