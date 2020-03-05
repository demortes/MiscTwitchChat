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
    public class BlameModule : ModuleBase<SocketCommandContext>
    {
        private IConfiguration _config;
        private IDiscordClient _client;

        public BlameModule(IConfiguration config, IDiscordClient discordClient)
        {
            _config = config;
            _client = discordClient;
        }

        [Command("blame")]
        public async Task Insult(IUser target = null)
        {
            if (target == null)
            {
                await ReplyAsync("Welp, you can't blame anyone but yourself apparently.");
                return;
            }

            await ReplyAsync(reply);
        }
    }
}
