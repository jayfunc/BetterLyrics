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

        public LyricsProvider? ToLyricsProvider()
        {
            return format switch
            {
                LyricsFormat.Lrc => LyricsProvider.LocalLrcFile,
                LyricsFormat.Eslrc => LyricsProvider.LocalEslrcFile,
                LyricsFormat.Ttml => LyricsProvider.LocalTtmlFile,
                _ => null
            };
        }
    }
}