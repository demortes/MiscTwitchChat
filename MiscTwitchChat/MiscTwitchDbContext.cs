using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
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

        public DbSet<Disconsenter> Disconsenters { get; set; }
    }
}
