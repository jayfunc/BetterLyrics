// 2025/6/23 by Zhe Fang

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
    /// <summary>
    /// Defines the <see cref="MusicSearchService" />
    /// </summary>
    public class MusicSearchService : IMusicSearchService
    {
        #region Fields

        /// <summary>
        /// Defines the _httpClient
        /// </summary>
        private readonly HttpClient _httpClient;

        /// <summary>
        /// Defines the _settingsService
        /// </summary>
        private readonly ISettingsService _settingsService;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="MusicSearchService"/> class.
        /// </summary>
        /// <param name="settingsService">The settingsService<see cref="ISettingsService"/></param>
        public MusicSearchService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add(
                "User-Agent",
                $"{AppInfo.AppName} {AppInfo.AppVersion} ({AppInfo.GithubUrl})"
            );
        }

        #endregion

        #region Methods

        /// <summary>
        /// The SearchAlbumArtAsync
        /// </summary>
        /// <param name="title">The title<see cref="string"/></param>
        /// <param name="artist">The artist<see cref="string"/></param>
        /// <returns>The <see cref="byte[]?"/></returns>
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
                        if (FuzzyMatch(Path.GetFileNameWithoutExtension(file), title, artist))
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

        /// <summary>
        /// The SearchLyricsAsync
        /// </summary>
        /// <param name="title">The title<see cref="string"/></param>
        /// <param name="artist">The artist<see cref="string"/></param>
        /// <param name="album">The album<see cref="string"/></param>
        /// <param name="durationMs">The durationMs<see cref="double"/></param>
        /// <param name="matchMode">The matchMode<see cref="MusicSearchMatchMode"/></param>
        /// <returns>The <see cref="Task{(string?, LyricsFormat?)}"/></returns>
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

        // 判断相似度

        /// <summary>
        /// The FuzzyMatch
        /// </summary>
        /// <param name="fileName">The fileName<see cref="string"/></param>
        /// <param name="title">The title<see cref="string"/></param>
        /// <param name="artist">The artist<see cref="string"/></param>
        /// <returns>The <see cref="bool"/></returns>
        private static bool FuzzyMatch(string fileName, string title, string artist)
        {
            var normFile = Normalize(fileName);
            var normTarget1 = Normalize(title + artist);
            var normTarget2 = Normalize(artist + title);

            int dist1 = LevenshteinDistance(normFile, normTarget1);
            int dist2 = LevenshteinDistance(normFile, normTarget2);

            return dist1 <= 3 || dist2 <= 3; // 阈值可调整
        }

        /// <summary>
        /// The LevenshteinDistance
        /// </summary>
        /// <param name="a">The a<see cref="string"/></param>
        /// <param name="b">The b<see cref="string"/></param>
        /// <returns>The <see cref="int"/></returns>
        private static int LevenshteinDistance(string a, string b)
        {
            if (string.IsNullOrEmpty(a))
                return b.Length;
            if (string.IsNullOrEmpty(b))
                return a.Length;
            int[,] d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++)
                d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++)
                d[0, j] = j;
            for (int i = 1; i <= a.Length; i++)
            for (int j = 1; j <= b.Length; j++)
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + (a[i - 1] == b[j - 1] ? 0 : 1)
                );
            return d[a.Length, b.Length];
        }

        /// <summary>
        /// The Normalize
        /// </summary>
        /// <param name="s">The s<see cref="string"/></param>
        /// <returns>The <see cref="string"/></returns>
        private static string Normalize(string s)
        {
            if (string.IsNullOrWhiteSpace(s))
                return "";
            var sb = new StringBuilder();
            foreach (var c in s.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(c))
                    sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// The SanitizeFileName
        /// </summary>
        /// <param name="fileName">The fileName<see cref="string"/></param>
        /// <param name="replacement">The replacement<see cref="char"/></param>
        /// <returns>The <see cref="string"/></returns>
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

        /// <summary>
        /// The LocalLyricsSearchInLyricsFiles
        /// </summary>
        /// <param name="title">The title<see cref="string"/></param>
        /// <param name="artist">The artist<see cref="string"/></param>
        /// <param name="format">The format<see cref="LyricsFormat"/></param>
        /// <returns>The <see cref="Task{string?}"/></returns>
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
                        if (FuzzyMatch(Path.GetFileNameWithoutExtension(file), title, artist))
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

        /// <summary>
        /// The LocalLyricsSearchInMusicFiles
        /// </summary>
        /// <param name="title">The title<see cref="string"/></param>
        /// <param name="artist">The artist<see cref="string"/></param>
        /// <returns>The <see cref="string?"/></returns>
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
                        if (FuzzyMatch(Path.GetFileNameWithoutExtension(file), title, artist))
                        {
                            //Track track = new(file);
                            //var test1 = track.Lyrics.SynchronizedLyrics;
                            //var test2 = track.Lyrics.UnsynchronizedLyrics;

                            try
                            {
                                var plain = TagLib.File.Create(file).Tag.Lyrics;
                                if (plain != null && plain != string.Empty)
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

        /// <summary>
        /// The ReadCache
        /// </summary>
        /// <param name="title">The title<see cref="string"/></param>
        /// <param name="artist">The artist<see cref="string"/></param>
        /// <param name="format">The format<see cref="LyricsFormat"/></param>
        /// <returns>The <see cref="string?"/></returns>
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

        /// <summary>
        /// The SearchLrcLib
        /// </summary>
        /// <param name="title">The title<see cref="string"/></param>
        /// <param name="artist">The artist<see cref="string"/></param>
        /// <param name="album">The album<see cref="string"/></param>
        /// <param name="duration">The duration<see cref="int"/></param>
        /// <param name="matchMode">The matchMode<see cref="MusicSearchMatchMode"/></param>
        /// <returns>The <see cref="Task{string?}"/></returns>
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

        /// <summary>
        /// The WriteCache
        /// </summary>
        /// <param name="title">The title<see cref="string"/></param>
        /// <param name="artist">The artist<see cref="string"/></param>
        /// <param name="lyrics">The lyrics<see cref="string"/></param>
        /// <param name="format">The format<see cref="LyricsFormat"/></param>
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

        #endregion
    }
}
