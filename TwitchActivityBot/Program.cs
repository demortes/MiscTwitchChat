using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

            serviceCollection.AddLogging(config => config.AddSerilog(dispose: true));
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            serviceCollection.AddMySql<ActivityBotDbContext>(
                configuration.GetConnectionString("DefaultConnection"),
                ServerVersion.AutoDetect(configuration.GetConnectionString("DefaultConnection"))
            );
            serviceCollection.AddScoped<Chatbot>();

            var services = serviceCollection.BuildServiceProvider();

            using (var scope = services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ActivityBotDbContext>();
                db.Database.Migrate();
            }

            var bot = services.GetRequiredService<Chatbot>();
            while (bot.isConnected())
                Thread.Sleep(5000);
        }
    }
}
