using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;
using TwitchLib.Api;

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
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables()
                .Build();
            var logger = loggerFactory.CreateLogger("TwitchActivityBot");
            serviceCollection.AddSingleton<ILogger>(logger);
            serviceCollection.AddSingleton<IConfiguration>(configuration);
            //Configure DB.
            var db = new ActivityBotDbContext();
            serviceCollection.AddSingleton(db);
            serviceCollection.AddScoped<Chatbot>();

            //Create TwitchAPI
            var twitchApi = new TwitchAPI(loggerFactory);
            twitchApi.Settings.ClientId = configuration.GetSection("TwitchAPI").GetValue<string>("ClientID");
            twitchApi.Settings.Secret = configuration.GetSection("TwitchAPI").GetValue<string>("ClientSecret");
            twitchApi.Settings.AccessToken = twitchApi.Helix.Channels.GetAccessToken();
            twitchApi.Settings.Scopes = new System.Collections.Generic.List<TwitchLib.Api.Core.Enums.AuthScopes>
            {
                TwitchLib.Api.Core.Enums.AuthScopes.Helix_Moderation_Read
            };
            var userId = twitchApi.Helix.Users.GetUsersAsync(logins: new System.Collections.Generic.List<string>
            {
                "demortes"
            }).Result.Users.First().Id;
            if(userId == null)
            {
                throw new System.Exception("Unable to connect to API. Aborting start up.");
            }
            serviceCollection.AddSingleton(twitchApi);

            //Check Config/Connection.
            //Configure twitch bot(s).
            var services = serviceCollection.BuildServiceProvider();
            var bot = services.GetRequiredService<Chatbot>();
            while (bot.isConnected())
                ;
        }
    }
}
