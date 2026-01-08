using BetterLyrics.WinUI3.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Models.DbContext
{
    public partial class PlayHistoryDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public PlayHistoryDbContext(DbContextOptions<PlayHistoryDbContext> options) : base(options) { }

        public DbSet<PlayHistoryItem> PlayHistory { get; set; }
    }
}
