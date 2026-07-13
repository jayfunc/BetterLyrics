using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;

namespace BetterLyrics.Core.Helpers;

public static class PathHelper
{
#if WINDOWS
    public static string LocalFolderPath => Windows.Storage.ApplicationData.Current.LocalFolder.Path;
    public static string CacheFolderPath => Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path;
#else
    private static string BaseDataFolderPath
    {
        get
        {
            if (OperatingSystem.IsBrowser())
            {
                return $"/data/{App.AppName}";
            }
            else
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), App.AppName);
            }
        }
    }
    public static string LocalFolderPath => Path.Combine(BaseDataFolderPath, "state");
    public static string CacheFolderPath => Path.Combine(BaseDataFolderPath, "cache");
#endif

    private static string AssetsFolderPath
    {
        get
        {
            return SystemHelper.CurrentFramework switch
            {
                SoftwareFramework.WinUI3 => Path.Combine("ms-appx:///", "Assets"),
                SoftwareFramework.Avalonia => Path.Combine("avares://BetterLyrics.Avalonia/", "Assets"),
                _ => throw new NotSupportedException("Unsupported framework type.")
            };
        }
    }

    public static string DocumentsFolderPath => Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    public static string SettingsDirectory => Path.Combine(LocalFolderPath, "settings");
    public static string SettingsFilePath => Path.Combine(SettingsDirectory, "settings.json");

    public static string AlbumArtPlaceholderPath => Path.Combine(AssetsFolderPath, "AlbumArtPlaceholder.png");

    public static string LogDirectory => Path.Combine(CacheFolderPath, "logs");
    public static string LogFilePattern => Path.Combine(LogDirectory, "log-.txt");

    public static string LyricsCacheDirectory => Path.Combine(CacheFolderPath, "lyrics");
    public static string AmllTtmlDbIndexPath => Path.Combine(LyricsCacheDirectory, "amll-ttml-db-index.jsonl");

    public static string AmllTtmlDbLastUpdatedPath =>
        Path.Combine(LyricsCacheDirectory, "amll-ttml-db-last-updated.txt");

    public static string AlbumArtCacheDirectory => Path.Combine(CacheFolderPath, "album-art");
    public static string LocalAlbumArtCacheDirectory => Path.Combine(AlbumArtCacheDirectory, "local");
    public static string iTunesAlbumArtCacheDirectory => Path.Combine(AlbumArtCacheDirectory, "itunes");
    public static string KugouAlbumArtCacheDirectory => Path.Combine(AlbumArtCacheDirectory, "kugou");
    public static string NeteaseAlbumArtCacheDirectory => Path.Combine(AlbumArtCacheDirectory, "netease");

    public static string PlayQueuePath => Path.Combine(LocalFolderPath, "play-queue.m3u");

    public static string PlayHistoryPath => Path.Combine(LocalFolderPath, "play-history.db");
    public static string FilesIndexPath => Path.Combine(LocalFolderPath, "files-index.db");
    public static string SongSearchMapPath => Path.Combine(LocalFolderPath, "song-search-map.db");
    public static string LyricsCachePath => Path.Combine(LyricsCacheDirectory, "lyrics-cache.db");

    public static string PlayHistoryLiteDbPath => Path.Combine(LocalFolderPath, "play-history.litedb");
    public static string FilesIndexLiteDbPath => Path.Combine(LocalFolderPath, "files-index.litedb");
    public static string SongSearchMapLiteDbPath => Path.Combine(LocalFolderPath, "song-search-map.litedb");
    public static string LyricsCacheLiteDbPath => Path.Combine(LyricsCacheDirectory, "lyrics-cache.litedb");

    public static string PluginsDirectory => Path.Combine(LocalFolderPath, "plugins");
    public static string PendingPluginsDirectory => Path.Combine(LocalFolderPath, "plugins-pending");


    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(SettingsDirectory);
        Directory.CreateDirectory(LogDirectory);
        Directory.CreateDirectory(LyricsCacheDirectory);

        Directory.CreateDirectory(LocalAlbumArtCacheDirectory);
        Directory.CreateDirectory(iTunesAlbumArtCacheDirectory);
        Directory.CreateDirectory(KugouAlbumArtCacheDirectory);
        Directory.CreateDirectory(NeteaseAlbumArtCacheDirectory);

        Directory.CreateDirectory(PluginsDirectory);
        Directory.CreateDirectory(PendingPluginsDirectory);
    }
}