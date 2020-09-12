using Microsoft.EntityFrameworkCore;
using MiscTwitchChat.Classlib.Entities;

namespace MiscTwitchChat
{
    public class MiscTwitchDbContext : DbContext
    {
        public MiscTwitchDbContext(DbContextOptions<MiscTwitchDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Setting>()
                .HasKey(p => new { p.Channel, p.Name });

            modelBuilder.Entity<CommandCount>()
                .HasKey(c => new { c.Channel, c.CommandUsed, c.TargetUser });
        }

        public DbSet<Disconsenter> Disconsenters { get; set; }
        public DbSet<ActiveChatter> ActiveChatters { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<CommandCount> CommandCounts { get; set; }
    }
}
