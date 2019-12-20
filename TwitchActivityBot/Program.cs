using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;

namespace TwitchActivityBot
{
    public class Program
    {
        static void Main(string[] args)
        {
            //Load configuration files
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", true, true)
                .AddUserSecrets<Program>()
                .AddEnvironmentVariables()
                .Build();
            //Configure DB.
            var db = new ActivityBotDbContext();
            //Check Config/Connection.
            //Configure twitch bot(s).
            var bot = new Chatbot(configuration, db);
            while (bot.isConnected())
                ;
        }
    }
}
