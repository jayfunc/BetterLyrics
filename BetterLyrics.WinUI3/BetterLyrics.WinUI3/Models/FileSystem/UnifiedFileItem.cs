using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Models.FileSystem
{
    public class UnifiedFileItem
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public long Size { get; set; }
        public bool IsFolder { get; set; }
        public DateTime? LastModified { get; set; }
    }
}
