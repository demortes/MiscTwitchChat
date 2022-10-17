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
using Microsoft.Extensions.Logging;

namespace DiscordBot.Services
{
    public class CommandHandlingService
    {
        private readonly DiscordSocketClient _discord;
        private readonly IServiceProvider _services;
        private ConcurrentDictionary<ulong, string> idDict = new ConcurrentDictionary<ulong, string>();
        private readonly InteractionService _interactionService;
        private readonly ILogger<CommandHandlingService> _logger;

        public CommandHandlingService(IServiceProvider services, InteractionService interactionService, ILogger<CommandHandlingService> logger, DiscordSocketClient discord)
        {
            _logger = logger;
            _interactionService = interactionService;
            _services = services;

            discord.InteractionCreated += async interaction =>
            {
                _logger.LogInformation("Interaction received, {interaction}", interaction);
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
