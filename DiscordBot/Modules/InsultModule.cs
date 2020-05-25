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
    public class InsultModule : ModuleBase<SocketCommandContext>
    {
        //private IConfiguration _config;

        //public InsultModule(IConfiguration config)
        //{
        //    _config = config;
        //}

        //[Command("insult")]
        //public async Task Insult(IUser target = null)
        //{
        //    var channel = Context.Channel.Name;
        //    var user = Context.User.Username;
        //    var url = _config.GetValue<string>("BaseAPIUrl");
        //    var apiService = new DemAPI.Client(url, new HttpClient());
        //    apiService.ReadResponseAsString = true;
        //    var reply = await apiService.ApiInsultAsync(channel, user, target?.Username);
        //    await ReplyAsync(reply);
        //}
    }
}
