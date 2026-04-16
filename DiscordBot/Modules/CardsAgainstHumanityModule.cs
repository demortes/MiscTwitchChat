using Discord;
using Discord.Interactions;
using DiscordBot.DemAPI;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    // Modules must be public and inherit from an IModuleBase
    public class CardsAgainstHumanityModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly Client _apiService;

        public CardsAgainstHumanityModule(Client apiService)
        {
            _apiService = apiService;
        }

        // Dependency Injection will fill this value in for us
        [SlashCommand("ping", "Is it alive?")]
        public Task PingAsync()
            => this.RespondAsync("pong!");

        // Get info on a user, or the user who invoked the command if one is not specified
        [SlashCommand("userinfo", "Get some information about a user.")]
        public async Task UserInfoAsync(IUser user = null)
        {
            user ??= Context.User;

            await this.RespondAsync(user.ToString());
        }

        [SlashCommand("cah", "Everyones favorite card game....")]
        public async Task CardAsync(string args = null)
        {
            var tts = !string.IsNullOrWhiteSpace(args) && args.ToLower() == "tts";
            var reply = await _apiService.ApiCardsGetAsync(Context.Channel?.Id.ToString());
            await RespondAsync(reply, isTTS: tts);
        }

        [SlashCommand("bancah", "Ban everyone's favorite card game.")]
        public async Task BanCah()
        {
            var reply = await _apiService.ApiCardsDeleteAsync(Context.Channel!.Id.ToString());
            await RespondAsync(text: reply);
        }
    }
}
