// 2025/6/23 by Zhe Fang

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Enums
{
    #region Enums

    /// <summary>
    /// Defines the LyricsFormat
    /// </summary>
    public enum LyricsFormat
    {
        /// <summary>
        /// Defines the Lrc
        /// </summary>
        Lrc,

        /// <summary>
        /// Defines the Eslrc
        /// </summary>
        Eslrc,

        /// <summary>
        /// Defines the Ttml
        /// </summary>
        Ttml,
        Qrc,
        Krc,
        NotSpecified,
    }

    #endregion

    /// <summary>
    /// Defines the <see cref="LyricsFormatExtensions" />
    /// </summary>
    public static class LyricsFormatExtensions
    {
        #region Methods

        /// <summary>
        /// The Detect
        /// </summary>
        /// <param name="content">The content<see cref="string"/></param>
        /// <returns>The <see cref="LyricsFormat?"/></returns>
        public static LyricsFormat? DetectFormat(this string content)
        {
            if (
                content.StartsWith("<?xml")
                && System.Text.RegularExpressions.Regex.IsMatch(content, @"<tt(:\w+)?\b")
            )
            {
                return LyricsFormat.Ttml;
            }
            // 检测标准LRC和增强型LRC
            else if (
                System.Text.RegularExpressions.Regex.IsMatch(content, @"\[\d{1,2}:\d{2}")
                || System.Text.RegularExpressions.Regex.IsMatch(
                    content,
                    @"<\d{1,2}:\d{2}\.\d{2,3}>"
                )
            )
            {
                return LyricsFormat.Lrc;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// The ToFileExtension
        /// </summary>
        /// <param name="format">The format<see cref="LyricsFormat"/></param>
        /// <returns>The <see cref="string"/></returns>
        public static string ToFileExtension(this LyricsFormat format)
        {
            return format switch
            {
                LyricsFormat.Lrc => ".lrc",
                LyricsFormat.Qrc => ".qrc",
                LyricsFormat.Krc => ".krc",
                LyricsFormat.Eslrc => ".eslrc",
                LyricsFormat.Ttml => ".ttml",
                _ => ".*",
            };
        }

        #endregion
    }
}
