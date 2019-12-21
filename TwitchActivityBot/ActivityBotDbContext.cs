using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiscTwitchChat.Classlib.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace TwitchActivityBot
{
    public class ActivityBotDbContext : DbContext
    {
        public DbSet<ActiveChatter> ActiveChatters { get; set; }

        public ActivityBotDbContext(DbContextOptions options) : base(options)
        {
        }
        public ActivityBotDbContext() : base()
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json", true).AddUserSecrets<Program>(true).AddEnvironmentVariables().Build();
            optionsBuilder.UseMySql(config.GetConnectionString("DefaultConnection"));
            base.OnConfiguring(optionsBuilder);
        }
    }
}
