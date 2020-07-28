using Discord;
using Discord.Commands;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class PickupModule : ModuleBase<SocketCommandContext>
    {
        private IConfiguration _config;

        public PickupModule(IConfiguration config)
        {
            _config = config;
        }

        [Command("pickup")]
        public async Task Pickup(IUser target = null)
        {
            var channel = Context.Channel.Name;
            var user = Context.User.Username;
            var url = _config.GetValue<string>("BaseAPIUrl");
            var apiService = new DemAPI.Client(url, new HttpClient());
            apiService.ReadResponseAsString = true;
            var reply = await apiService.ApiPickupAsync();
            await ReplyAsync(reply);
        }
    }
}
