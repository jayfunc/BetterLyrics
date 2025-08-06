using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;

namespace BetterLyrics.WinUI3.Helper
{
    public class PathHelper
    {
        private static string LocalFolder => ApplicationData.Current.LocalFolder.Path;
        public static string CacheFolder => ApplicationData.Current.LocalCacheFolder.Path;
        public static string AssetsFolder => Path.Combine(Package.Current.InstalledPath, "Assets");

        //public static string LanguageProfilePath => Path.Combine(AssetsFolder, "Core14.profile.xml");
        public static string LanguageProfilePath => Path.Combine(AssetsFolder, "Wiki82.profile.xml");
        public static string LogoPath => Path.Combine(AssetsFolder, "Logo.ico");
        public static string AIMPLogoPath => Path.Combine(AssetsFolder, "AIMP.png");
        public static string Foobar2000LogoPath => Path.Combine(AssetsFolder, "foobar2000.png");
        public static string MusicBeeLogoPath => Path.Combine(AssetsFolder, "MusicBee.png");
        public static string SpotifyLogoPath => Path.Combine(AssetsFolder, "Spotify.png");
        public static string AppleMusicLogoPath => Path.Combine(AssetsFolder, "AppleMusic.png");
        public static string iTunesLogoPath => Path.Combine(AssetsFolder, "iTunes.png");
        public static string KugouMusicLogoPath => Path.Combine(AssetsFolder, "KugouMusic.png");
        public static string NetEaseCloudMusicLogoPath => Path.Combine(AssetsFolder, "NetEaseCloudMusic.png");
        public static string QQMusicLogoPath => Path.Combine(AssetsFolder, "QQMusic.png");
        public static string LXMusicLogoPath => Path.Combine(AssetsFolder, "LXMusic.png");
        public static string MediaPlayerWindows11LogoPath => Path.Combine(AssetsFolder, "MediaPlayerWindows11.png");
        public static string PotPlayerLogoPath => Path.Combine(AssetsFolder, "PotPlayer.png");
        public static string ChromeLogoPath => Path.Combine(AssetsFolder, "Chrome.png");
        public static string EdgeLogoPath => Path.Combine(AssetsFolder, "Edge.png");
        public static string UnknownPlayerLogoPath => Path.Combine(AssetsFolder, "Question.png");

        public static string LogDirectory => Path.Combine(CacheFolder, "logs");
        public static string LogFilePattern => Path.Combine(LogDirectory, "log-.txt");

        public static string LyricsCacheDirectory => Path.Combine(CacheFolder, "lyrics");

        public static string LrcLibLyricsCacheDirectory => Path.Combine(LyricsCacheDirectory, "lrclib");
        public static string NeteaseLyricsCacheDirectory => Path.Combine(LyricsCacheDirectory, "netease");
        public static string QQLyricsCacheDirectory => Path.Combine(LyricsCacheDirectory, "qq");
        public static string KugouLyricsCacheDirectory => Path.Combine(LyricsCacheDirectory, "kugou");
        public static string AmllTtmlDbLyricsCacheDirectory => Path.Combine(LyricsCacheDirectory, "amll-ttml-db");
        public static string AmllTtmlDbIndexPath => Path.Combine(LyricsCacheDirectory, "amll-ttml-db-index.json");
        public static string AmllTtmlDbLastUpdatedPath => Path.Combine(LyricsCacheDirectory, "amll-ttml-db-last-updated.txt");

        public static string TranslationCacheDirectory => Path.Combine(CacheFolder, "translations");

        public static string QQTranslationCacheDirectory => Path.Combine(TranslationCacheDirectory, "qq");
        public static string NeteaseTranslationCacheDirectory => Path.Combine(TranslationCacheDirectory, "netease");

        public static string AlbumArtCacheDirectory => Path.Combine(CacheFolder, "album-art");

        public static string iTunesAlbumArtCacheDirectory => Path.Combine(AlbumArtCacheDirectory, "itunes");

        public static void EnsureDirectories()
        {
            Directory.CreateDirectory(LogDirectory);

            Directory.CreateDirectory(LrcLibLyricsCacheDirectory);
            Directory.CreateDirectory(QQLyricsCacheDirectory);
            Directory.CreateDirectory(KugouLyricsCacheDirectory);
            Directory.CreateDirectory(NeteaseLyricsCacheDirectory);
            Directory.CreateDirectory(AmllTtmlDbLyricsCacheDirectory);

            Directory.CreateDirectory(QQTranslationCacheDirectory);
            Directory.CreateDirectory(NeteaseTranslationCacheDirectory);

            Directory.CreateDirectory(iTunesAlbumArtCacheDirectory);
        }
    }
}
