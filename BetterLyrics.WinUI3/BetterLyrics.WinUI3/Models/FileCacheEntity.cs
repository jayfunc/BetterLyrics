using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Models
{
    [Table("FileCache")]
    public class FileCacheEntity : UnifiedFileItem
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public string ParentPath { get; set; }

        [Indexed(Unique = true)]
        public string FullPath { get; set; }

        public string Title { get; set; }
    }
}
