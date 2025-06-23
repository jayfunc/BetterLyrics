using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ATL;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace BetterLyrics.WinUI3.Services
{
    public class MusicSearchService : IMusicSearchService
    {
        private readonly HttpClient _httpClient;
        private readonly ISettingsService _settingsService;

        public MusicSearchService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add(
                "User-Agent",
                $"{AppInfo.AppName} {AppInfo.AppVersion} ({AppInfo.GithubUrl})"
            );
        }

        public byte[]? SearchAlbumArtAsync(string title, string artist)
        {
            foreach (var folder in _settingsService.LocalLyricsFolders)
            {
                if (Directory.Exists(folder.Path) && folder.IsEnabled)
                {
                    foreach (
                        var file in Directory.GetFiles(
                            folder.Path,
                            $"*.*",
                            SearchOption.AllDirectories
                        )
                    )
                    {
                        if (file.Contains(title) && file.Contains(artist))
                        {
                            Track track = new(file);
                            var bytes = track.EmbeddedPictures.FirstOrDefault()?.PictureData;
                            if (bytes != null)
                            {
                                return bytes;
                            }
                        }
                    }
                }
            }

            return null;
        }

        public async Task<(string?, LyricsFormat?)> SearchLyricsAsync(
            string title,
            string artist,
            string album = "",
            double durationMs = 0.0,
            MusicSearchMatchMode matchMode = MusicSearchMatchMode.TitleAndArtist
        )
        {
            foreach (var provider in _settingsService.LyricsSearchProvidersInfo)
            {
                if (!provider.IsEnabled)
                {
                    continue;
                }

                switch (provider.Provider)
                {
                    case LyricsSearchProvider.LrcLib:
                        // Check cache first
                        var cachedLyrics = ReadCache(title, artist, LyricsFormat.Lrc);
                        if (!string.IsNullOrWhiteSpace(cachedLyrics))
                        {
                            return (cachedLyrics, LyricsFormat.Lrc);
                        }
                        break;
                    default:
                        break;
                }

                string? searchedLyrics = null;

                switch (provider.Provider)
                {
                    case LyricsSearchProvider.LocalMusicFile:
                        searchedLyrics = LocalLyricsSearchInMusicFiles(title, artist);
                        break;
                    case LyricsSearchProvider.LocalLrcFile:
                        searchedLyrics = await LocalLyricsSearchInLyricsFiles(
                            title,
                            artist,
                            LyricsFormat.Lrc
                        );
                        break;
                    case LyricsSearchProvider.LocalEslrcFile:
                        searchedLyrics = await LocalLyricsSearchInLyricsFiles(
                            title,
                            artist,
                            LyricsFormat.Eslrc
                        );
                        break;
                    case LyricsSearchProvider.LocalTtmlFile:
                        searchedLyrics = await LocalLyricsSearchInLyricsFiles(
                            title,
                            artist,
                            LyricsFormat.Ttml
                        );
                        break;
                    case LyricsSearchProvider.LrcLib:
                        searchedLyrics = await SearchLrcLib(
                            title,
                            artist,
                            album,
                            (int)(durationMs / 1000),
                            matchMode
                        );
                        break;
                    default:
                        break;
                }

                if (!string.IsNullOrWhiteSpace(searchedLyrics))
                {
                    switch (provider.Provider)
                    {
                        case LyricsSearchProvider.LrcLib:
                            WriteCache(title, artist, searchedLyrics, LyricsFormat.Lrc);
                            return (searchedLyrics, LyricsFormat.Lrc);
                        case LyricsSearchProvider.LocalMusicFile:
                            return (searchedLyrics, LyricsFormatExtensions.Detect(searchedLyrics));
                        case LyricsSearchProvider.LocalLrcFile:
                            return (searchedLyrics, LyricsFormat.Lrc);
                        case LyricsSearchProvider.LocalEslrcFile:
                            return (searchedLyrics, LyricsFormat.Eslrc);
                        case LyricsSearchProvider.LocalTtmlFile:
                            return (searchedLyrics, LyricsFormat.Ttml);
                        default:
                            break;
                    }
                }
            }

            return (null, null);
        }

        private string? LocalLyricsSearchInMusicFiles(string title, string artist)
        {
            foreach (var folder in _settingsService.LocalLyricsFolders)
            {
                if (Directory.Exists(folder.Path) && folder.IsEnabled)
                {
                    foreach (
                        var file in Directory.GetFiles(
                            folder.Path,
                            $"*.*",
                            SearchOption.AllDirectories
                        )
                    )
                    {
                        if (file.Contains(title) && file.Contains(artist))
                        {
                            try
                            {
                                // TODO: replace TagLib with ATL or another library that supports AOT
                                string plain = TagLib.File.Create(file).Tag.Lyrics;
                                if (plain != string.Empty)
                                {
                                    return plain;
                                }
                            }
                            catch (Exception) { }
                        }
                    }
                }
            }

            return null;
        }

        private async Task<string?> LocalLyricsSearchInLyricsFiles(
            string title,
            string artist,
            LyricsFormat format
        )
        {
            foreach (var folder in _settingsService.LocalLyricsFolders)
            {
                if (Directory.Exists(folder.Path) && folder.IsEnabled)
                {
                    foreach (
                        var file in Directory.GetFiles(
                            folder.Path,
                            $"*{format.ToFileExtension()}",
                            SearchOption.AllDirectories
                        )
                    )
                    {
                        if (file.Contains(title) && file.Contains(artist))
                        {
                            string? raw = await File.ReadAllTextAsync(
                                file,
                                FileHelper.GetEncoding(file)
                            );
                            if (raw != null)
                            {
                                return raw;
                            }
                        }
                    }
                }
            }
            return null;
        }

        private async Task<string?> SearchLrcLib(
            string title,
            string artist,
            string album,
            int duration,
            MusicSearchMatchMode matchMode
        )
        {
            // Build API query URL
            var url =
                $"https://lrclib.net/api/search?"
                + $"track_name={Uri.EscapeDataString(title)}&"
                + $"artist_name={Uri.EscapeDataString(artist)}";

            if (matchMode == MusicSearchMatchMode.TitleArtistAlbumAndDuration)
            {
                url +=
                    $"&album_name={Uri.EscapeDataString(album)}"
                    + $"&durationMs={Uri.EscapeDataString(duration.ToString())}";
            }

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            var jArr = JsonSerializer.Deserialize(
                json,
                Serialization.SourceGenerationContext.Default.JsonElement
            );
            if (jArr.ValueKind == JsonValueKind.Array && jArr.GetArrayLength() > 0)
            {
                var first = jArr[0];
                var syncedLyrics = first.GetProperty("syncedLyrics").GetString();
                var result = string.IsNullOrWhiteSpace(syncedLyrics) ? null : syncedLyrics;
                if (!string.IsNullOrWhiteSpace(result))
                {
                    return result;
                }
            }

            return null;
        }

        private void WriteCache(string title, string artist, string lyrics, LyricsFormat format)
        {
            var safeArtist = SanitizeFileName(artist);
            var safeTitle = SanitizeFileName(title);
            var cacheFilePath = Path.Combine(
                AppInfo.OnlineLyricsCacheDirectory,
                $"{safeArtist} - {safeTitle}{format.ToFileExtension()}"
            );
            File.WriteAllText(cacheFilePath, lyrics);
        }

        private string? ReadCache(string title, string artist, LyricsFormat format)
        {
            var safeArtist = SanitizeFileName(artist);
            var safeTitle = SanitizeFileName(title);
            var cacheFilePath = Path.Combine(
                AppInfo.OnlineLyricsCacheDirectory,
                $"{safeArtist} - {safeTitle}{format.ToFileExtension()}"
            );
            if (File.Exists(cacheFilePath))
            {
                return File.ReadAllText(cacheFilePath);
            }
            return null;
        }

        private static string SanitizeFileName(string fileName, char replacement = '_')
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sb = new StringBuilder(fileName.Length);
            foreach (var c in fileName)
            {
                sb.Append(Array.IndexOf(invalidChars, c) >= 0 ? replacement : c);
            }
            return sb.ToString();
        }
    }
}
