using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DiscordBot.Services;
using Microsoft.Extensions.Configuration;

namespace DiscordBot.Modules
{
    // Modules must be public and inherit from an IModuleBase
    public class CardsAgainstHumanityModule : ModuleBase<SocketCommandContext>
    {
        private readonly IConfiguration _config;

        public CardsAgainstHumanityModule(IConfiguration config)
        {
            _config = config;
        }
        // Dependency Injection will fill this value in for us
        [Command("ping")]
        [Alias("pong", "hello")]
        public Task PingAsync()
            => ReplyAsync("pong!");

        // Get info on a user, or the user who invoked the command if one is not specified
        [Command("userinfo")]
        public async Task UserInfoAsync(IUser user = null)
        {
            user = user ?? Context.User;

            await ReplyAsync(user.ToString());
        }

        //[Command("cah")]
        //[Alias("cards", "card")]
        //public async Task CardAsync(string args = null)
        //{
        //    var tts = false;
        //    if (!string.IsNullOrWhiteSpace(args) && args.ToLower() == "tts")
        //        tts = true;
        //    var url = _config.GetValue<string>("BaseAPIUrl");
        //    var apiService = new DemAPI.Client(url, new HttpClient());
        //    apiService.ReadResponseAsString = true;
        //    var reply = await apiService.ApiCardsAsync();
        //    await ReplyAsync(reply, isTTS: tts);
        //}

        //// Ban a user
        //[Command("ban")]
        //[RequireContext(ContextType.Guild)]
        //// make sure the user invoking the command can ban
        //[RequireUserPermission(GuildPermission.BanMembers)]
        //// make sure the bot itself can ban
        //[RequireBotPermission(GuildPermission.BanMembers)]
        //public async Task BanUserAsync(IGuildUser user, [Remainder] string reason = null)
        //{
        //    await user.Guild.AddBanAsync(user, reason: reason);
        //    await ReplyAsync("ok!");
        //}

        //// [Remainder] takes the rest of the command's arguments as one argument, rather than splitting every space
        //[Command("echo")]
        //public Task EchoAsync([Remainder] string text)
        //    // Insert a ZWSP before the text to prevent triggering other bots!
        //    => ReplyAsync('\u200B' + text);

        //// 'params' will parse space-separated elements into a list
        //[Command("list")]
        //public Task ListAsync(params string[] objects)
        //    => ReplyAsync("You listed: " + string.Join("; ", objects));

        //// Setting a custom ErrorMessage property will help clarify the precondition error
        //[Command("guild_only")]
        //[RequireContext(ContextType.Guild, ErrorMessage = "Sorry, this command must be ran from within a server, not a DM!")]
        //public Task GuildOnlyCommand()
        //    => ReplyAsync("Nothing to see here!");
    }
}
