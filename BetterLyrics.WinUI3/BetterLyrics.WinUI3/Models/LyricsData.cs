// 2025/6/23 by Zhe Fang

using System.Collections.Generic;

namespace BetterLyrics.WinUI3.Models
{
    /// <summary>
    /// Defines the <see cref="LyricsData" />
    /// </summary>
    public class LyricsData
    {
        #region Properties

        /// <summary>
        /// Gets or sets the LanguageIndex
        /// </summary>
        public int LanguageIndex { get; set; } = 0;

        /// <summary>
        /// Gets the LyricsLines
        /// </summary>
        public List<LyricsLine> LyricsLines => MultiLangLyricsLines[LanguageIndex];

        /// <summary>
        /// Gets or sets the MultiLangLyricsLines
        /// </summary>
        public List<List<LyricsLine>> MultiLangLyricsLines { get; set; } = [];

        #endregion
    }
}
