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
    public class DadModule : ModuleBase<SocketCommandContext>
    {
        private IConfiguration _config;

        public DadModule(IConfiguration config)
        {
            _config = config;
        }

        [Command("dad")]
        public async Task Insult(IUser target = null)
        {
            var channel = Context.Channel.Name;
            var user = Context.User.Username;
            var url = _config.GetValue<string>("BaseAPIUrl");
            var apiService = new DemAPI.Client(url, new HttpClient());
            apiService.ReadResponseAsString = true;
            var reply = await apiService.ApiDadAsync();
            await ReplyAsync(reply);
        }
    }
}
