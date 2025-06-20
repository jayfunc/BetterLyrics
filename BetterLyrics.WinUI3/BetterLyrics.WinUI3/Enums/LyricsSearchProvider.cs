using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Enums
{
    public enum LyricsSearchProvider
    {
        LocalLrcFile,
        LocalMusicFile,
        LrcLib,
        QQMusic,
        KugouMusic,
    }

    public static class OnlineLyricsProviderExtensions
    {
        public static LyricsFormat ToLyricsFormat(this LyricsSearchProvider provider)
        {
            return provider switch
            {
                LyricsSearchProvider.LocalLrcFile => LyricsFormat.Lrc,
                LyricsSearchProvider.LocalMusicFile => LyricsFormat.Lrc,
                LyricsSearchProvider.LrcLib => LyricsFormat.Lrc,
                LyricsSearchProvider.QQMusic => LyricsFormat.DecryptedQrc,
                LyricsSearchProvider.KugouMusic => LyricsFormat.DecryptedKrc,
                _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null),
            };
        }
    }
}
