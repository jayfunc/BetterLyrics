using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Models.Db
{
    public partial class FilesIndexDbContext : DbContext
    {
        public FilesIndexDbContext(DbContextOptions<FilesIndexDbContext> options) : base(options) { }

        public DbSet<FilesIndexItem> FilesIndex { get; set; }
    }
}
