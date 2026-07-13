using BetterLyrics.Core.Enums;

namespace BetterLyrics.Core.Extensions;

public static class LyricsFormatExtensions
{
    extension(LyricsFormat format)
    {
        public string ToFileExtension()
        {
            return format switch
            {
                LyricsFormat.Lrc => ".lrc",
                LyricsFormat.Qrc => ".qrc",
                LyricsFormat.Krc => ".krc",
                LyricsFormat.Eslrc => ".eslrc",
                LyricsFormat.Ttml => ".ttml",
                _ => ".*"
            };
        }

        public LyricsSearchProvider? ToLyricsSearchProvider()
        {
            return format switch
            {
                LyricsFormat.Lrc => LyricsSearchProvider.LocalLrcFile,
                LyricsFormat.Eslrc => LyricsSearchProvider.LocalEslrcFile,
                LyricsFormat.Ttml => LyricsSearchProvider.LocalTtmlFile,
                _ => null
            };
        }
    }
}