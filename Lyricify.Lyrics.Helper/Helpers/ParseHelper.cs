using Lyricify.Lyrics.Models;

namespace Lyricify.Lyrics.Helpers
{
    /// <summary>
    /// 解析帮助类
    /// </summary>
    public static class ParseHelper
    {
        /// <summary>
        /// 解析歌词
        /// </summary>
        /// <param name="lyrics">歌词字符串</param>
        /// <returns>解析后的歌词数据</returns>
        public static LyricsData? ParseLyrics(string lyrics)
        {
            var type = TypeHelper.GetLyricsTypes(lyrics);
            if (type != LyricsRawTypes.Unknown)
            {
                ParseLyrics(lyrics, type);
            }
            return null;
        }

        /// <summary>
        /// 解析歌词
        /// </summary>
        /// <param name="lyrics">歌词字符串</param>
        /// <param name="lyricsRawType">该歌词字符串的原始类型</param>
        /// <returns>解析后的歌词数据</returns>
        public static LyricsData? ParseLyrics(string lyrics, LyricsRawTypes lyricsRawType)
        {
            return lyricsRawType switch
            {
                LyricsRawTypes.LyricifySyllable => Parsers.LyricifySyllableParser.Parse(lyrics),
                LyricsRawTypes.LyricifyLines => Parsers.LyricifyLinesParser.Parse(lyrics),
                LyricsRawTypes.Lrc => Parsers.LrcParser.Parse(lyrics),
                LyricsRawTypes.Qrc => Parsers.QrcParser.Parse(lyrics),
                LyricsRawTypes.Krc => Parsers.KrcParser.Parse(lyrics),
                LyricsRawTypes.Yrc => Parsers.YrcParser.Parse(lyrics),
                LyricsRawTypes.Ttml => Parsers.TtmlParser.Parse(lyrics),
                LyricsRawTypes.Spotify => Parsers.SpotifyParser.Parse(lyrics),
                LyricsRawTypes.Musixmatch => Parsers.MusixmatchParser.Parse(lyrics),
                _ => null,
            };
        }
    }
}
