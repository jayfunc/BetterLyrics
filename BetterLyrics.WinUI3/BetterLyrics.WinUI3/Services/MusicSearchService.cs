using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ATL;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using Lyricify.Lyrics.Providers;

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
                    case LyricsSearchProvider.QQMusic:
                    case LyricsSearchProvider.KugouMusic:
                        // Check cache first
                        var cachedLyrics = ReadCache(title, artist, provider.Provider);
                        if (!string.IsNullOrWhiteSpace(cachedLyrics))
                        {
                            return (cachedLyrics, provider.Provider.ToLyricsFormat());
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
                            provider.Provider.ToLyricsFormat()
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
                    case LyricsSearchProvider.QQMusic:
                        searchedLyrics = await SearchQQMusic(
                            title,
                            artist,
                            album,
                            (int)durationMs,
                            matchMode
                        );
                        break;
                    case LyricsSearchProvider.KugouMusic:
                        break;
                    default:
                        break;
                }

                if (!string.IsNullOrWhiteSpace(searchedLyrics))
                {
                    switch (provider.Provider)
                    {
                        case LyricsSearchProvider.LrcLib:
                        case LyricsSearchProvider.QQMusic:
                        case LyricsSearchProvider.KugouMusic:
                            WriteCache(title, artist, searchedLyrics, provider.Provider);
                            break;
                        default:
                            break;
                    }
                    return (searchedLyrics, provider.Provider.ToLyricsFormat());
                }
            }

            return (null, null);
        }

        private string? LocalLyricsSearchInMusicFiles(string title, string artist)
        {
            foreach (var path in _settingsService.MusicLibraries)
            {
                if (Directory.Exists(path))
                {
                    foreach (
                        var file in Directory.GetFiles(path, $"*.*", SearchOption.AllDirectories)
                    )
                    {
                        if (file.Contains(title) && file.Contains(artist))
                        {
                            Track track = new(file);
                            if (track.Lyrics.SynchronizedLyrics.Count > 0)
                            {
                                // Get synchronized lyrics from the track (metadata)
                                var lrc = track.Lyrics.FormatSynchToLRC();
                                if (lrc != null)
                                {
                                    return lrc;
                                }
                            }
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
            foreach (var path in _settingsService.MusicLibraries)
            {
                if (Directory.Exists(path))
                {
                    foreach (
                        var file in Directory.GetFiles(
                            path,
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

            var jArr = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);
            if (jArr is not null && jArr.Count > 0)
            {
                var syncedLyrics = jArr![0]?.syncedLyrics?.ToString();
                var result = string.IsNullOrWhiteSpace(syncedLyrics) ? null : syncedLyrics;
                if (!string.IsNullOrWhiteSpace(result))
                {
                    return result;
                }
            }

            return null;
        }

        private async Task<string?> SearchQQMusic(
            string title,
            string artist,
            string album,
            int durationMs,
            MusicSearchMatchMode matchMode
        )
        {
            string? queryId = (
                (
                    await new Lyricify.Lyrics.Searchers.QQMusicSearcher().SearchForResult(
                        new Lyricify.Lyrics.Models.TrackMultiArtistMetadata()
                        {
                            DurationMs =
                                matchMode == MusicSearchMatchMode.TitleArtistAlbumAndDuration
                                    ? durationMs
                                    : null,
                            Album =
                                matchMode == MusicSearchMatchMode.TitleArtistAlbumAndDuration
                                    ? album
                                    : null,
                            AlbumArtists = [artist],
                            Artists = [artist],
                            Title = title,
                        }
                    )
                ) as Lyricify.Lyrics.Searchers.QQMusicSearchResult
            )?.Id;
            if (queryId is string id)
            {
                return (await Lyricify.Lyrics.Decrypter.Qrc.Helper.GetLyricsAsync(id))?.Lyrics;
            }
            return null;
        }

        private void WriteCache(
            string title,
            string artist,
            string lyrics,
            LyricsSearchProvider provider
        )
        {
            var safeArtist = SanitizeFileName(artist);
            var safeTitle = SanitizeFileName(title);
            var cacheFilePath = Path.Combine(
                AppInfo.OnlineLyricsCacheDirectory,
                $"{safeArtist} - {safeTitle}{provider.ToLyricsFormat().ToFileExtension()}"
            );
            File.WriteAllText(cacheFilePath, lyrics);
        }

        private string? ReadCache(string title, string artist, LyricsSearchProvider provider)
        {
            var safeArtist = SanitizeFileName(artist);
            var safeTitle = SanitizeFileName(title);
            var cacheFilePath = Path.Combine(
                AppInfo.OnlineLyricsCacheDirectory,
                $"{safeArtist} - {safeTitle}{provider.ToLyricsFormat().ToFileExtension()}"
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
