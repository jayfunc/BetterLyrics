// 2025/6/23 by Zhe Fang

using System.Collections.Generic;

namespace BetterLyrics.WinUI3.Models
{
    public class LyricsData
    {
        public int LanguageIndex { get; set; } = 0;

        public List<LyricsLine> LyricsLines => MultiLangLyricsLines[LanguageIndex];

        public List<List<LyricsLine>> MultiLangLyricsLines { get; set; } = [];
    }
}
