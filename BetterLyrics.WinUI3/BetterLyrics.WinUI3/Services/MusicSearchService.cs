// 2025/6/23 by Zhe Fang

using ATL;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using CommunityToolkit.Mvvm.DependencyInjection;
using iTunesSearch.Library;
using Lyricify.Lyrics.Providers.Web.Kugou;
using Lyricify.Lyrics.Searchers;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services
{
    public class MusicSearchService : IMusicSearchService
    {
        private readonly HttpClient _amllTtmlDbHttpClient;

        private readonly HttpClient _lrcLibHttpClient;

        private readonly HttpClient _iTunesHttpClinet;

        private readonly iTunesSearchManager _iTunesSearchManager;

        private readonly ISettingsService _settingsService;
        private readonly ILogger _logger;

        public MusicSearchService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _logger = Ioc.Default.GetRequiredService<ILogger<MusicSearchService>>();

            _lrcLibHttpClient = new();
            _lrcLibHttpClient.DefaultRequestHeaders.Add(
                "User-Agent",
                $"{AppInfo.AppName} {AppInfo.AppVersion} ({AppInfo.GithubUrl})"
            );
            _amllTtmlDbHttpClient = new();
            _iTunesHttpClinet = new();
            _iTunesSearchManager = new();
        }

        public async Task<bool> DownloadAmllTtmlDbIndexAsync()
        {
            const string url = "https://raw.githubusercontent.com/Steve-xmh/amll-ttml-db/refs/heads/main/metadata/raw-lyrics-index.jsonl";
            try
            {
                using var response = await _amllTtmlDbHttpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode) return false;

                await using var stream = await response.Content.ReadAsStreamAsync();
                await using var fs = new FileStream(
                    AppInfo.AmllTtmlDbIndexPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None
                );
                await stream.CopyToAsync(fs);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string GuessCountryCode(string album, string artist)
        {
            string s = album + artist;
            if (s.Any(c => c >= 0x4e00 && c <= 0x9fff)) // 中文
                return "cn";
            if (s.Any(c => (c >= 0x3040 && c <= 0x30ff) || (c >= 0x31f0 && c <= 0x31ff))) // 日文
                return "jp";
            if (s.Any(c => c >= 0xac00 && c <= 0xd7af)) // 韩文
                return "kr";
            if (s.Any(c => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))) // 英文
                return "us";
            // 其他情况
            return "us";
        }

        public async Task<byte[]> SearchAlbumArtAsync(string title, string artist, string album)
        {
            foreach (var folder in _settingsService.LocalLyricsFolders)
            {
                if (Directory.Exists(folder.Path) && folder.IsEnabled)
                {
                    foreach (var file in Directory.GetFiles(folder.Path, $"*.*", SearchOption.AllDirectories))
                    {
                        if (MusicMatch(Path.GetFileNameWithoutExtension(file), title, artist))
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

            var resultItems = await _iTunesSearchManager.GetAlbumsAsync(album, 1, countryCode: GuessCountryCode(album, artist));
            var url = resultItems.Albums.Where(al => Normalize(al.ArtistName).Contains(Normalize(artist)))
                .FirstOrDefault()?.ArtworkUrl100.Replace("100x100bb.jpg", "100000x100000-999.jpg");
            if (url != null)
            {
                return await _iTunesHttpClinet.GetByteArrayAsync(url);
            }

            return await ImageHelper.CreateTextPlaceholderBytesAsync($"{artist} - {title}", 400, 400);
        }

        public async Task<string?> SearchLyricsAsync(string title, string artist, string album, double durationMs, CancellationToken token)
        {
            _logger.LogInformation("Searching lyrics for: {Title} - {Artist} (Album: {Album}, Duration: {DurationMs}ms)", title, artist, album, durationMs);

            foreach (var provider in _settingsService.LyricsSearchProvidersInfo)
            {
                if (!provider.IsEnabled)
                {
                    continue;
                }

                string? cachedLyrics;
                LyricsFormat lyricsFormat = provider.Provider.GetLyricsFormat();

                // Check cache first
                if (provider.Provider.IsRemote())
                {
                    cachedLyrics = ReadCache(title, artist, lyricsFormat, provider.Provider.GetCacheDirectory());
                    if (!string.IsNullOrWhiteSpace(cachedLyrics))
                    {
                        return cachedLyrics;
                    }
                }

                string? searchedLyrics = null;

                if (provider.Provider.IsLocal())
                {
                    if (provider.Provider == LyricsSearchProvider.LocalMusicFile)
                    {
                        searchedLyrics = LocalLyricsSearchInMusicFiles(title, artist);
                    }
                    else
                    {
                        searchedLyrics = await LocalLyricsSearchInLyricsFiles(title, artist, lyricsFormat);
                    }
                }
                else
                {
                    switch (provider.Provider)
                    {
                        case LyricsSearchProvider.LrcLib:
                            searchedLyrics = await SearchLrcLibAsync(title, artist, album, (int)(durationMs / 1000));
                            break;
                        case LyricsSearchProvider.QQ:
                            searchedLyrics = await SearchUsingLyricifyAsync(title, artist, album, (int)durationMs, Searchers.QQMusic);
                            break;
                        case LyricsSearchProvider.Kugou:
                            searchedLyrics = await SearchUsingLyricifyAsync(title, artist, album, (int)durationMs, Searchers.Kugou);
                            break;
                        case LyricsSearchProvider.Netease:
                            searchedLyrics = await SearchUsingLyricifyAsync(title, artist, album, (int)durationMs, Searchers.Netease);
                            break;
                        case LyricsSearchProvider.AmllTtmlDb:
                            searchedLyrics = await SearchAmllTtmlDbAsync(title, artist);
                            break;
                        default:
                            break;
                    }
                }

                token.ThrowIfCancellationRequested();

                if (!string.IsNullOrWhiteSpace(searchedLyrics))
                {
                    if (provider.Provider.IsRemote())
                    {
                        WriteCache(title, artist, searchedLyrics, lyricsFormat, provider.Provider.GetCacheDirectory());
                    }

                    return searchedLyrics;
                }
            }

            return null;
        }

        private static bool MusicMatch(string fileName, string title, string artist)
        {
            var normFileName = Normalize(fileName);
            var normTitle = Normalize(title);
            var normArtist = Normalize(artist);

            // 常见两种顺序
            return normFileName == normTitle + normArtist
                || normFileName == normArtist + normTitle;
        }

        // 预处理：去除空格、括号、下划线、横杠、点、大小写等
        static string Normalize(string s) =>
            new string(s
                .Where(c => char.IsLetterOrDigit(c))
                .ToArray())
                .ToLowerInvariant();

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

        private async Task<string?> LocalLyricsSearchInLyricsFiles(string title, string artist, LyricsFormat format)
        {
            foreach (var folder in _settingsService.LocalLyricsFolders)
            {
                if (Directory.Exists(folder.Path) && folder.IsEnabled)
                {
                    foreach (var file in Directory.GetFiles(folder.Path, $"*{format.ToFileExtension()}", SearchOption.AllDirectories))
                    {
                        if (MusicMatch(Path.GetFileNameWithoutExtension(file), title, artist))
                        {
                            string? raw = await File.ReadAllTextAsync(file, FileHelper.GetEncoding(file));
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

        private string? LocalLyricsSearchInMusicFiles(string title, string artist)
        {
            foreach (var folder in _settingsService.LocalLyricsFolders)
            {
                if (Directory.Exists(folder.Path) && folder.IsEnabled)
                {
                    foreach (var file in Directory.GetFiles(folder.Path, $"*.*", SearchOption.AllDirectories))
                    {
                        if (MusicMatch(Path.GetFileNameWithoutExtension(file), title, artist))
                        {
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

        private string? ReadCache(string title, string artist, LyricsFormat format, string cacheFolderPath)
        {
            var safeArtist = SanitizeFileName(artist);
            var safeTitle = SanitizeFileName(title);
            var cacheFilePath = Path.Combine(cacheFolderPath, $"{safeArtist} - {safeTitle}{format.ToFileExtension()}");
            if (File.Exists(cacheFilePath))
            {
                return File.ReadAllText(cacheFilePath);
            }
            return null;
        }

        private async Task<string?> SearchAmllTtmlDbAsync(string title, string artist)
        {
            // 检索本地 JSONL 索引文件，查找 rawLyricFile
            if (!File.Exists(AppInfo.AmllTtmlDbIndexPath))
            {
                var downloadOk = await DownloadAmllTtmlDbIndexAsync();
                if (!downloadOk || !File.Exists(AppInfo.AmllTtmlDbIndexPath))
                    return null;
            }

            string? rawLyricFile = null;
            await foreach (var line in File.ReadLinesAsync(AppInfo.AmllTtmlDbIndexPath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                try
                {
                    using var doc = JsonDocument.Parse(line);
                    var root = doc.RootElement;
                    if (!root.TryGetProperty("metadata", out var metadataArr))
                        continue;
                    string? musicName = null;
                    string? artists = null;
                    foreach (var meta in metadataArr.EnumerateArray())
                    {
                        if (meta.GetArrayLength() != 2)
                            continue;
                        var key = meta[0].GetString();
                        var valueArr = meta[1];
                        if (key == "musicName" && valueArr.GetArrayLength() > 0)
                            musicName = valueArr[0].GetString();
                        if (key == "artists" && valueArr.GetArrayLength() > 0)
                            artists = valueArr[0].GetString();
                    }
                    if (musicName == null || artists == null)
                        continue;

                    if (MusicMatch($"{artists} - {musicName}", title, artist))
                    {
                        if (root.TryGetProperty("rawLyricFile", out var rawLyricFileProp))
                        {
                            rawLyricFile = rawLyricFileProp.GetString();
                            break;
                        }
                    }
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(rawLyricFile))
                return null;

            // 下载歌词内容
            var url = $"https://raw.githubusercontent.com/Steve-xmh/amll-ttml-db/refs/heads/main/raw-lyrics/{rawLyricFile}";
            try
            {
                var response = await _amllTtmlDbHttpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return null;
                return await response.Content.ReadAsStringAsync();
            }
            catch
            {
                return null;
            }
        }

        private async Task<string?> SearchLrcLibAsync(string title, string artist, string album, int duration)
        {
            // Build API query URL
            var url =
                $"https://lrclib.net/api/search?" +
                $"track_name={Uri.EscapeDataString(title)}&" +
                $"artist_name={Uri.EscapeDataString(artist)}&" +
                $"&album_name={Uri.EscapeDataString(album)}" +
                $"&durationMs={Uri.EscapeDataString(duration.ToString())}";

            var response = await _lrcLibHttpClient.GetAsync(url);
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

        private async Task<string?> SearchUsingLyricifyAsync(
            string title,
            string artist,
            string album,
            int durationMs,
            Searchers searchers
        )
        {
            var result = await SearchersHelper.GetSearcher(searchers).SearchForResult(
                new Lyricify.Lyrics.Models.TrackMultiArtistMetadata()
                {
                    DurationMs = durationMs,
                    Album = album,
                    Artists = [artist],
                    Title = title,
                }
            );

            if (result is QQMusicSearchResult qqResult)
            {
                var response = await Lyricify.Lyrics.Helpers.ProviderHelper.QQMusicApi.GetLyricsAsync(qqResult.Id);
                var original = response?.Lyrics;
                return original;
            }
            else if (result is NeteaseSearchResult neteaseResult)
            {
                var response = await Lyricify.Lyrics.Helpers.ProviderHelper.NeteaseApi.GetLyric(neteaseResult.Id);
                return response?.Lrc.Lyric;
            }
            else if (result is KugouSearchResult kugouResult)
            {
                var response = await Lyricify.Lyrics.Helpers.ProviderHelper.KugouApi.GetSearchLyrics(hash: kugouResult.Hash);
                if (response?.Candidates.FirstOrDefault() is SearchLyricsResponse.Candidate candidate)
                {
                    return Lyricify.Lyrics.Decrypter.Krc.Helper.GetLyrics(
                        candidate.Id,
                        candidate.AccessKey
                    );
                }
            }

            return null;
        }

        private void WriteCache(
            string title,
            string artist,
            string lyrics,
            LyricsFormat format,
            string cacheFolderPath
        )
        {
            var safeArtist = SanitizeFileName(artist);
            var safeTitle = SanitizeFileName(title);
            var cacheFilePath = Path.Combine(
                cacheFolderPath,
                $"{safeArtist} - {safeTitle}{format.ToFileExtension()}"
            );
            File.WriteAllText(cacheFilePath, lyrics);
        }
    }
}
