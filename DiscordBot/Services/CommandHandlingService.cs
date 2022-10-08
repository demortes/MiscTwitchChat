using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Collections.Concurrent;
using System.Linq;
using Discord.Interactions;

namespace DiscordBot.Services
{
    public class CommandHandlingService
    {
        private readonly CommandService _commands;
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;
        private ConcurrentDictionary<ulong, string> idDict = new ConcurrentDictionary<ulong, string>();
        private readonly InteractionService _interactionService;

        public CommandHandlingService(IServiceProvider services, InteractionService interactionService)
        {
            _commands = services.GetRequiredService<CommandService>();
            _discord = services.GetRequiredService<DiscordSocketClient>();
            _services = services;

            _interactionService = new InteractionService(_discord);
            _interactionService = interactionService;

            _discord.InteractionCreated += async interaction =>
            {
                var ctx = new SocketInteractionContext(_discord, interaction);
                await _interactionService.ExecuteCommandAsync(ctx, _services);
            };
        }

        public async Task InitializeAsync()
        {
            // Register modules that are public and inherit ModuleBase<T>.
            await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            await _interactionService.RegisterCommandsGloballyAsync(true);
        }
    }
}
