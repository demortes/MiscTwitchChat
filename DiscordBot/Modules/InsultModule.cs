using Discord;
using Discord.Interactions;
using DiscordBot.DemAPI;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class InsultModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly Client _apiService;

        public InsultModule(Client apiService)
        {
            _apiService = apiService;
        }

        [SlashCommand("insult", "Insult someone. Either tag them or let the bot pick one randomly.")]
        public async Task Insult(IUser target = null)
        {
            var reply = await _apiService.ApiInsultAsync(Context.Channel.Name, Context.User.Username, target?.Username);
            await RespondAsync(reply);
        }
    }
}
