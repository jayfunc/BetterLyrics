using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models
{
    public class LyricsData
    {
        public int LanguageIndex { get; set; } = 0;

        public List<LyricsLine> LyricsLines => MultiLangLyricsLines[LanguageIndex];
        public List<List<LyricsLine>> MultiLangLyricsLines { get; set; } = [];
    }
}
