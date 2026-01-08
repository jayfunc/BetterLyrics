using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Models.DbContext
{
    public partial class LyricsCacheDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public LyricsCacheDbContext(DbContextOptions<LyricsCacheDbContext> options) : base(options) { }

        public DbSet<LyricsCacheItem> LyricsCache { get; set; } 
    }
}
