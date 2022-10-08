using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
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
        [Alias("pong", "hello")]
        public Task PingAsync()
            => RespondAsync("pong!");

        // Get info on a user, or the user who invoked the command if one is not specified
        [SlashCommand("userinfo", "Get some information about a user.")]
        public async Task UserInfoAsync(IUser user = null)
        {
            user ??= Context.User;

            await RespondAsync(user.ToString());
        }

        [SlashCommand("cah", "Everyones favorite card game....")]
        [Alias("cards", "card")]
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
        [Alias(new string[] { "throwcahintothevoid" })]
        public async Task BanCah()
        {
            var url = _config.GetValue<string>("BaseAPIUrl");
            var apiService = new DemAPI.Client(url, new HttpClient())
            {
                ReadResponseAsString = true
            };
            var reply = await apiService.ApiCardsDeleteAsync(Context.Channel!.Id.ToString());
            await RespondAsync(reply);
        }
    }
}
