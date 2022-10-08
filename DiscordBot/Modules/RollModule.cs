using Discord.Commands;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class RollModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly ILogger<RollModule> _logger;

        public RollModule(ILogger<RollModule> logger)
        {
            _logger = logger;
        }

        [SlashCommand("roll", "Roll a number of dice, using the XdY format, ex: 1d20")]
        public async Task RollAsync(string argument)
        {
            _logger.LogInformation("User {username} initiated the roll command with args {args}", Context.User.Username, argument);
            var regEx = new Regex("^(?<numOfDice>\\d+)d(?<numOfSides>\\d+)$");
            var match = regEx.Match(argument);
            if (match.Success)
            {
                var numOfDice = int.Parse(match.Groups["numOfDice"].Value);
                var numOfSides = int.Parse(match.Groups["numOfSides"].Value);
                var total = 0;
                var rand = new Random();
                for (int i = 0; i < numOfDice; i++)
                {
                    total += rand.Next(1, numOfSides);
                }

                await RespondAsync($"{Context.User.Username} has rolled {total}");
            }
            else
            {
                await RespondAsync($"{Context.User.Username} has given incorrect parameters. Please try again. Beep boop.");
            }
        }
    }
}
