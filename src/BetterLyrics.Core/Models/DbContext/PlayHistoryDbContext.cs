using BetterLyrics.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BetterLyrics.Core.Models.DbContext;

public class PlayHistoryDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public PlayHistoryDbContext(DbContextOptions<PlayHistoryDbContext> options) : base(options)
    {
    }

    public DbSet<PlayHistoryItem> PlayHistory { get; set; }
}