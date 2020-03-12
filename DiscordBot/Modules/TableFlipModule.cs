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
    public class TableFlipModule : ModuleBase<SocketCommandContext>
    {
        private IConfiguration _config;

        public TableFlipModule(IConfiguration config)
        {
            _config = config;
        }

        [Command("(╯°□°）╯︵ ┻━┻")]
        public async Task TableFlip()
        {
            await ReplyAsync("┬─┬ノ(ಠ_ಠノ)");
        }
    }
}
