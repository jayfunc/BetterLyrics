using BetterLyrics.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BetterLyrics.Core.Models.DbContext
{
    public partial class SongSearchMapDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public DbSet<MappedSongSearchQuery> SongSearchMap { get; set; }

        public SongSearchMapDbContext(DbContextOptions<SongSearchMapDbContext> options) : base(options) { }
    }
}
