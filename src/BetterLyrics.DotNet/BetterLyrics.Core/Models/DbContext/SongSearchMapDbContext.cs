using BetterLyrics.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BetterLyrics.Core.Models.DbContext;

public class SongSearchMapDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public SongSearchMapDbContext(DbContextOptions<SongSearchMapDbContext> options) : base(options)
    {
    }

    public DbSet<MappedSongSearchQuery> SongSearchMap { get; set; }
}