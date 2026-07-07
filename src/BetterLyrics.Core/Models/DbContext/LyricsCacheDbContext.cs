using BetterLyrics.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BetterLyrics.Core.Models.DbContext;

public class LyricsCacheDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public LyricsCacheDbContext(DbContextOptions<LyricsCacheDbContext> options) : base(options)
    {
    }

    public DbSet<LyricsCacheItem> LyricsCache { get; set; }
}