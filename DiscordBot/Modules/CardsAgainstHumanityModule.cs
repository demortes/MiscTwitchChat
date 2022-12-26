using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Configuration;

namespace DiscordBot.Modules
{
    // Modules must be public and inherit from an IModuleBase
    public class CardsAgainstHumanityModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly IConfiguration _config;

        public CardsAgainstHumanityModule(IConfiguration config)
        {
            _config = config;
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
            var tts = false;
            if (!string.IsNullOrWhiteSpace(args) && args.ToLower() == "tts")
                tts = true;
            string url = _config.GetValue<string>("BaseAPIUrl");
            var apiService = new DemAPI.Client(url, new HttpClient())
            {
                ReadResponseAsString = true
            };
            var reply = await apiService.ApiCardsGetAsync(Context.Channel?.Id.ToString());
            await RespondAsync(reply, isTTS: tts);
        }

        [SlashCommand("bancah", "Ban everyone's favorite card game.")]
        public async Task BanCah()
        {
            var url = _config.GetValue<string>("BaseAPIUrl");
            var apiService = new DemAPI.Client(url, new HttpClient())
            {
                ReadResponseAsString = true
            };
            var reply = await apiService.ApiCardsDeleteAsync(Context.Channel!.Id.ToString());
            await RespondAsync(text: reply);
        }
    }
}
