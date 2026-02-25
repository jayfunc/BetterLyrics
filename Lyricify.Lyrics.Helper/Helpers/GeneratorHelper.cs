namespace Lyricify.Lyrics.Helpers
{
    /// <summary>
    /// 生成帮助类
    /// </summary>
    public static class GenerateHelper
    {
        /// <summary>
        /// 生成歌词字符串
        /// </summary>
        /// <param name="lyrics">用于生成的源歌词数据</param>
        /// <param name="lyricsType">需要生成的歌词字符串的类型</param>
        /// <returns>生成出的歌词字符串, <see langword="null"/> 若为空或生成失败</returns>
        public static string? GenerateString(Models.LyricsData lyrics, Models.LyricsTypes lyricsType)
        {
            var result = lyricsType switch
            {
                Models.LyricsTypes.LyricifySyllable => Generators.LyricifySyllableGenerator.Generate(lyrics),
                Models.LyricsTypes.LyricifyLines => Generators.LyricifyLinesGenerator.Generate(lyrics),
                Models.LyricsTypes.Lrc => Generators.LrcGenerator.Generate(lyrics),
                Models.LyricsTypes.Qrc => Generators.QrcGenerator.Generate(lyrics),
                Models.LyricsTypes.Krc => Generators.KrcGenerator.Generate(lyrics),
                Models.LyricsTypes.Yrc => Generators.YrcGenerator.Generate(lyrics),
                _ => null,
            };
            if (string.IsNullOrWhiteSpace(result)) return null;
            return result;
        }
    }
}
