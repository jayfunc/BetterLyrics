using Lyricify.Lyrics.Models;

namespace Lyricify.Lyrics.Helpers
{
    public static class TypeHelper
    {
        /// <summary>
        /// 识别歌词的类型
        /// </summary>
        /// <param name="lyrics">歌词字符串</param>
        /// <returns><see cref="LyricsRawTypes"/>, 如果没有识别成功则会返回 <see cref="LyricsRawTypes.Unknown"/>.</returns>
        public static LyricsRawTypes GetLyricsTypes(string lyrics)
        {
            return LyricsRawTypes.Unknown;
        }

        /// <summary>
        /// 将 LyricsRawType 转换为 LyricsType
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static LyricsTypes GetLyricsType(this LyricsRawTypes type) => type switch
        {
            LyricsRawTypes.Unknown => LyricsTypes.Unknown,
            LyricsRawTypes.LyricifySyllable => LyricsTypes.LyricifySyllable,
            LyricsRawTypes.LyricifyLines => LyricsTypes.LyricifyLines,
            LyricsRawTypes.Lrc => LyricsTypes.Lrc,
            LyricsRawTypes.Qrc => LyricsTypes.Qrc,
            LyricsRawTypes.QrcFull => LyricsTypes.Qrc,
            LyricsRawTypes.Krc => LyricsTypes.Krc,
            LyricsRawTypes.Yrc => LyricsTypes.Yrc,
            LyricsRawTypes.YrcFull => LyricsTypes.Yrc,
            LyricsRawTypes.Ttml => LyricsTypes.Ttml,
            LyricsRawTypes.AppleJson => LyricsTypes.Ttml,
            LyricsRawTypes.Spotify => LyricsTypes.Spotify,
            LyricsRawTypes.Musixmatch => LyricsTypes.Musixmatch,
            _ => LyricsTypes.Unknown,
        };

        /// <summary>
        /// 字符串是否是指定的歌词类型
        /// </summary>
        /// <param name="lyrics">歌词字符串</param>
        /// <param name="type">歌词类型</param>
        public static bool IsLyricsType(string lyrics, LyricsTypes type)
        {
            return type switch
            {
                LyricsTypes.LyricifyLines => Types.LyricifyLines.IsLyricifyLines(lyrics),
                LyricsTypes.Lrc => Types.Lrc.IsLrc(lyrics),
                _ => false, // 暂不支持类型判断的，返回 false
            };
        }

        /// <summary>
        /// 字符串的歌词类型是否在指定类型列表中
        /// </summary>
        /// <param name="lyrics">歌词字符串</param>
        /// <param name="types">歌词类型列表</param>
        public static bool IsLyricsType(string lyrics, LyricsTypes[] types)
        {
            if (types.Length < 1) return false;

            foreach (var type in types)
            {
                if (IsLyricsType(lyrics, type))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
