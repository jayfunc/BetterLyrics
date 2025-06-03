using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models {
    public class MetadataIndex {
        [PrimaryKey]
        public string Path { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
    }
}
