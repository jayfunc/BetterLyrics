using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Models.DbContext
{
    public partial class SongSearchMapDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public DbSet<MappedSongSearchQuery> SongSearchMap { get; set; }

        public SongSearchMapDbContext(DbContextOptions<SongSearchMapDbContext> options) : base(options) { }
    }
}
