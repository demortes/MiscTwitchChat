using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace TwitchActivityBot
{
    public class Program
    {
        static void Main(string[] args)
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            ILoggerFactory loggerFactory = LoggerFactory.Create(builder=>
            {
                builder.AddConsole();
            });
            //Load configuration files
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .AddUserSecrets<Program>(false)
                .AddEnvironmentVariables()
                .Build();
            var logger = loggerFactory.CreateLogger("TwitchActivityBot");
            serviceCollection.AddSingleton<ILogger>(logger);
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            //Configure DB.
            var db = new ActivityBotDbContext();
            serviceCollection.AddDbContext<ActivityBotDbContext>();
            serviceCollection.AddSingleton(db);
            serviceCollection.AddScoped<Chatbot>();
            //Check Config/Connection.
            //Configure twitch bot(s).
            var services = serviceCollection.BuildServiceProvider();
            var bot = services.GetRequiredService<Chatbot>();
            while (bot.isConnected())
                ;
        }
    }
}
