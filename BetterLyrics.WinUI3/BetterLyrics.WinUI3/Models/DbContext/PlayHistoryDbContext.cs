using BetterLyrics.WinUI3.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BetterLyrics.WinUI3.Models.DbContext
{
    public partial class PlayHistoryDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public PlayHistoryDbContext(DbContextOptions<PlayHistoryDbContext> options) : base(options) { }

        public DbSet<PlayHistoryItem> PlayHistory { get; set; }
    }
}
