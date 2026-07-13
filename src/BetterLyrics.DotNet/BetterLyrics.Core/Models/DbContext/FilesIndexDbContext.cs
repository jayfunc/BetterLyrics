using BetterLyrics.Core.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace BetterLyrics.Core.Models.DbContext;

public class FilesIndexDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public FilesIndexDbContext(DbContextOptions<FilesIndexDbContext> options) : base(options)
    {
    }

    public DbSet<FilesIndexItem> FilesIndex { get; set; }
}