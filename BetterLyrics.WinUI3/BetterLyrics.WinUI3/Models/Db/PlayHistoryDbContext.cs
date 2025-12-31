using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Models.Db
{
    public partial class PlayHistoryDbContext : DbContext
    {
        public PlayHistoryDbContext(DbContextOptions<PlayHistoryDbContext> options) : base(options) { }

        public DbSet<PlayHistoryItem> PlayHistory { get; set; }
    }
}
