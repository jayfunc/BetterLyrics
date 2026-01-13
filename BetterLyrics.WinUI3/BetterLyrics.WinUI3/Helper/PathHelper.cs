using System.IO;
using Windows.ApplicationModel;
using Windows.Storage;

namespace BetterLyrics.WinUI3.Helper
{
    public class PathHelper
    {
        public static string LocalFolder => ApplicationData.Current.LocalFolder.Path;
        public static string CacheFolder => ApplicationData.Current.LocalCacheFolder.Path;
        public static string AssetsFolder => Path.Combine(Package.Current.InstalledPath, "Assets");

        public static string SettingsDirectory => Path.Combine(LocalFolder, "settings");
        public static string SettingsFilePath => Path.Combine(SettingsDirectory, "settings.json");

        public static string LanguageProfilePath => Path.Combine(AssetsFolder, "Wiki82.profile.xml");
        public static string LogoPath => Path.Combine(AssetsFolder, "Logo.ico");
        public static string AlbumArtPlaceholderPath => Path.Combine(AssetsFolder, "AlbumArtPlaceholder.png");
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
        public static string SaltPlayerForWindowsLogoPath => Path.Combine(AssetsFolder, "SaltPlayerForWindows.png");
        public static string MoeKoeMusicLogoPath => Path.Combine(AssetsFolder, "MoeKoeMusic.png");
        public static string Listen1LogoPath => Path.Combine(AssetsFolder, "Listen1.png");
        public static string OriginalSoundHQPlayerLogoPath => Path.Combine(AssetsFolder, "OriginalSoundHQPlayer.png");
        public static string UnknownPlayerLogoPath => Path.Combine(AssetsFolder, "Question.png");

        public static string LogDirectory => Path.Combine(CacheFolder, "logs");
        public static string LogFilePattern => Path.Combine(LogDirectory, "log-.txt");

        public static string LyricsCacheDirectory => Path.Combine(CacheFolder, "lyrics");
        public static string AmllTtmlDbIndexPath => Path.Combine(LyricsCacheDirectory, "amll-ttml-db-index.jsonl");
        public static string AmllTtmlDbLastUpdatedPath => Path.Combine(LyricsCacheDirectory, "amll-ttml-db-last-updated.txt");

        public static string AlbumArtCacheDirectory => Path.Combine(CacheFolder, "album-art");
        public static string iTunesAlbumArtCacheDirectory => Path.Combine(AlbumArtCacheDirectory, "itunes");
        public static string LocalAlbumArtCacheDirectory => Path.Combine(AlbumArtCacheDirectory, "local");

        public static string PlayQueuePath => Path.Combine(LocalFolder, "play-queue.m3u");
        public static string PlayHistoryPath => Path.Combine(LocalFolder, "play-history.db");
        public static string FilesIndexPath => Path.Combine(LocalFolder, "files-index.db");
        public static string SongSearchMapPath => Path.Combine(LocalFolder, "song-search-map.db");
        public static string LyricsCachePath => Path.Combine(LyricsCacheDirectory, "lyrics-cache.db");

        public static string PluginsDirectory => Path.Combine(LocalFolder, "plugins");
        public static string PendingPluginsDirectory => Path.Combine(LocalFolder, "plugins-pending");


        public static void EnsureDirectories()
        {
            Directory.CreateDirectory(SettingsDirectory);
            Directory.CreateDirectory(LogDirectory);
            Directory.CreateDirectory(LyricsCacheDirectory);
            Directory.CreateDirectory(iTunesAlbumArtCacheDirectory);
            Directory.CreateDirectory(LocalAlbumArtCacheDirectory);

            Directory.CreateDirectory(PluginsDirectory);
            Directory.CreateDirectory(PendingPluginsDirectory);
        }

    }
}