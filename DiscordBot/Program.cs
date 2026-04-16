using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBot.DemAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Context;
using Serilog.Formatting.Compact;
using System;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordBot
{
    class Program
    {
        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
                .AddEnvironmentVariables()
                .AddUserSecrets<Program>(true)
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(config)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Console(new CompactJsonFormatter())
                .CreateLogger();

            using (var services = ConfigureServices(config))
            {
                var loggingService = services.GetRequiredService<LoggingService>();
                var logger = Log.ForContext<Program>();
                var client = services.GetRequiredService<DiscordSocketClient>();
                var interactionService = services.GetRequiredService<InteractionService>();

                interactionService.SlashCommandExecuted += (info, ctx, result) =>
                {
                    using (LogContext.PushProperty("Command", info?.Name ?? "unknown"))
                    using (LogContext.PushProperty("Username", ctx.User.Username))
                    using (LogContext.PushProperty("UserId", ctx.User.Id))
                    using (LogContext.PushProperty("Guild", ctx.Guild?.Name ?? "DM"))
                    using (LogContext.PushProperty("GuildId", ctx.Guild?.Id.ToString() ?? "DM"))
                    using (LogContext.PushProperty("Channel", ctx.Channel.Name))
                    {
                        if (result.IsSuccess)
                            logger.Information("Slash command {Command} executed by {Username} in {Guild}/{Channel}",
                                info?.Name, ctx.User.Username, ctx.Guild?.Name ?? "DM", ctx.Channel.Name);
                        else
                            logger.Warning("Slash command {Command} failed for {Username}: {ErrorReason}",
                                info?.Name, ctx.User.Username, result.ErrorReason);
                    }
                    return Task.CompletedTask;
                };

                client.InteractionCreated += async (x) =>
                {
                    var ctx = new SocketInteractionContext(client, x);
                    await interactionService.ExecuteCommandAsync(ctx, services);
                };

                client.Ready += async () =>
                {
                    await interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), services);
                    await interactionService.RegisterCommandsGloballyAsync(true);
                };

                await client.LoginAsync(TokenType.Bot, config.GetValue<string>("Discord:Token"));
                await client.StartAsync();

                await Task.Delay(-1);
            }
        }

        private ServiceProvider ConfigureServices(IConfiguration config)
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<CommandService>()
                .AddSingleton<LoggingService>()
                .AddSingleton(config)
                .AddLogging(logging => logging.AddSerilog(dispose: true))
                .AddHttpClient()
                .AddSingleton(x =>
                {
                    var url = config.GetValue<string>("BaseAPIUrl");
                    var httpClient = x.GetRequiredService<IHttpClientFactory>().CreateClient();
                    return new Client(url, httpClient) { ReadResponseAsString = true };
                })
                .BuildServiceProvider();
        }
    }
}
