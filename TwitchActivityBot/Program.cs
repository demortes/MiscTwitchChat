using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace TwitchActivityBot
{
    public class Program
    {
        static void Main(string[] args)
        {
            IServiceCollection serviceCollection = new ServiceCollection();

            //Load configuration files
            var configurationBuilder = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables();
            if(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                configurationBuilder.AddUserSecrets<Program>();
            }

            var configuration = configurationBuilder.Build();
            serviceCollection.AddLogging(config =>
            {
                config.AddConfiguration(configuration);
                config.AddConsole();
            });
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            //Configure DB.
            serviceCollection.AddMySql<ActivityBotDbContext>(
                configuration.GetConnectionString("DefaultConnection"), 
                ServerVersion.AutoDetect(configuration.GetConnectionString("DefaultConnection"))
            );
            serviceCollection.AddScoped<Chatbot>();
            //Check Config/Connection.
            //Configure twitch bot(s).
            var services = serviceCollection.BuildServiceProvider();
            var bot = services.GetRequiredService<Chatbot>();
            while (bot.isConnected())
                Thread.Sleep(5000);
        }
    }
}
