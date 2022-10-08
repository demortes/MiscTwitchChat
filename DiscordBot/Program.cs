using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using DiscordBot.Services;
using Microsoft.Extensions.Configuration;
using Discord.Interactions;

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
                var client = services.GetRequiredService<DiscordSocketClient>();

                client.Log += LogAsync;
                services.GetRequiredService<CommandService>().Log += LogAsync;

                // Tokens should be considered secret data and never hard-coded.
                // We can read from the environment variable to avoid hardcoding.
                await client.LoginAsync(TokenType.Bot, config.GetValue<String>("Discord:Token"));
                await client.StartAsync();

                // Here we initialize the logic required to register our commands.
                client.Ready += async () => 
                { 
                    await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
                };

                await Task.Delay(-1);
            }
        }

        private Task LogAsync(LogMessage log)
        {
            if (log.Severity < LogSeverity.Info)
                Console.Error.WriteLine(log.ToString());
            else
                Console.WriteLine(log.ToString());

            return Task.CompletedTask;
        }

        private ServiceProvider ConfigureServices(IConfiguration config)
        {
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<InteractionService>()
                .AddSingleton<HttpClient>()
                .AddLogging()
                .AddSingleton(config)
                .BuildServiceProvider();
        }
    }
}
