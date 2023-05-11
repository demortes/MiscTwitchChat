using Microsoft.EntityFrameworkCore;
using MiscTwitchChat.Classlib.Entities;

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
    }
}
