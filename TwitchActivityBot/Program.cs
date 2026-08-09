using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiscTwitchChat.Health;
using Serilog;
using Serilog.Formatting.Compact;
using System;
using System.Threading;

namespace TwitchActivityBot
{
    public class Program
    {
        static void Main(string[] args)
        {
            IServiceCollection serviceCollection = new ServiceCollection();

            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables();
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                configurationBuilder.AddUserSecrets<Program>();
            }

            var configuration = configurationBuilder.Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Console(new CompactJsonFormatter())
                .CreateLogger();

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            // Detected once and reused, so the health host does not open a second probe connection at startup.
            var serverVersion = ServerVersion.AutoDetect(connectionString);

            serviceCollection.AddLogging(config => config.AddSerilog(dispose: true));
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.AddMySql<ActivityBotDbContext>(connectionString, serverVersion);
            serviceCollection.AddScoped<Chatbot>();

            var services = serviceCollection.BuildServiceProvider();

            using (var scope = services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ActivityBotDbContext>();
                db.Database.Migrate();
            }

            var bot = services.GetRequiredService<Chatbot>();

            using var health = StartHealthEndpoint(configuration, connectionString, serverVersion, bot);

            while (bot.isConnected())
                Thread.Sleep(5000);
        }

        /// <summary>
        /// Exposes this bot's health over HTTP so the container has something to probe. The bot writes every
        /// chat message it sees to MySQL and cannot do that while disconnected from Twitch, so both are
        /// readiness dependencies alongside the API.
        /// </summary>
        private static WebApplication StartHealthEndpoint(
            IConfiguration configuration,
            string connectionString,
            ServerVersion serverVersion,
            Chatbot bot)
        {
            return HealthEndpoint.Start(configuration, checks =>
            {
                // The health host gets its own context registration rather than sharing the bot's: that one is
                // long lived and written from Twitch event callbacks, and DbContext is not thread safe.
                checks.Services.AddMySql<ActivityBotDbContext>(connectionString, serverVersion);
                checks.AddDbContextConnectionCheck<ActivityBotDbContext>("mysql", HealthEndpoint.ReadyTag);

                checks.AddConnectionCheck(
                    "twitch",
                    bot.isConnected,
                    "Connected to Twitch chat.",
                    "Not connected to Twitch chat.",
                    HealthEndpoint.ReadyTag);

                var apiHealthUri = HealthEndpoint.ResolveApiHealthUri(configuration);
                if (apiHealthUri == null)
                {
                    Log.Warning("BaseAPIUrl is not configured, so the API health check is disabled.");
                    return;
                }

                checks.AddApiCheck("api", apiHealthUri, HealthEndpoint.ResolveApiTimeout(configuration), HealthEndpoint.ReadyTag);
            });
        }
    }
}
