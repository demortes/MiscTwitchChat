using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MiscTwitchChat.Classlib.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
        }

        public DbSet<Disconsenter> Disconsenters { get; set; }
        public DbSet<ActiveChatter> ActiveChatters { get; set; }
        public DbSet<Setting> Settings { get; set; }
    }
}
