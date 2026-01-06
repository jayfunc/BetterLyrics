using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Models.Db
{
    public partial class SongSearchMapDbContext : DbContext
    {
        public DbSet<MappedSongSearchQuery> SongSearchMap { get; set; }

        public SongSearchMapDbContext(DbContextOptions<SongSearchMapDbContext> options) : base(options) { }
    }
}
