using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBot.DemAPI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordBot
{
    class Program
    {
        // There is no need to implement IDisposable like before as we are
        // using dependency injection, which handles calling Dispose for us.
        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            //Config the configuration
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", true)
                .AddEnvironmentVariables()
                .AddUserSecrets<Program>(true)
                .Build();
            // You should dispose a service provider created using ASP.NET
            // when you are finished using it, at the end of your app's lifetime.
            // If you use another dependency injection framework, you should inspect
            // its documentation for the best way to do this.
            using (var services = ConfigureServices(config))
            {
                var loggingService = services.GetRequiredService<LoggingService>();
                var logger = services.GetRequiredService<ILogger<Program>>();
                var client = services.GetRequiredService<DiscordSocketClient>();
                var interactionService = services.GetRequiredService<InteractionService>();

                interactionService.SlashCommandExecuted += (info, ctx, result) =>
                {
                    using (logger.BeginScope(new Dictionary<string, object>
                    {
                        ["Username"] = ctx.User.Username,
                        ["UserId"] = ctx.User.Id,
                        ["Guild"] = ctx.Guild?.Name ?? "DM",
                        ["GuildId"] = ctx.Guild?.Id.ToString() ?? "DM",
                        ["Channel"] = ctx.Channel.Name,
                        ["Command"] = info?.Name ?? "unknown"
                    }))
                    {
                        if (result.IsSuccess)
                            logger.LogInformation("Slash command {Command} executed by {Username} in {Guild}/{Channel}",
                                info?.Name, ctx.User.Username, ctx.Guild?.Name ?? "DM", ctx.Channel.Name);
                        else
                            logger.LogWarning("Slash command {Command} failed for {Username}: {ErrorReason}",
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

                // Tokens should be considered secret data and never hard-coded.
                // We can read from the environment variable to avoid hardcoding.
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
                .AddLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConfiguration(config.GetSection("Logging"));
                    logging.AddJsonConsole(options =>
                    {
                        options.IncludeScopes = true;
                        options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
                    });
                })
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
