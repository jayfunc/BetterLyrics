// 2025/6/23 by Zhe Fang

using ATL;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Lyricify.Lyrics.Providers.Web.Kugou;
using Lyricify.Lyrics.Searchers;
using Microsoft.Extensions.Logging;
using NTextCat.Commons;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace BetterLyrics.WinUI3.Services.LyricsSearchService
{
    public class LyricsSearchService : ILyricsSearchService
    {
        private readonly HttpClient _amllTtmlDbHttpClient;
        private readonly HttpClient _lrcLibHttpClient;

        private readonly ISettingsService _settingsService;
        private readonly ILogger _logger;

        public LyricsSearchService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _logger = Ioc.Default.GetRequiredService<ILogger<LyricsSearchService>>();

            _lrcLibHttpClient = new();
            _lrcLibHttpClient.DefaultRequestHeaders.Add(
                "User-Agent",
                $"{Constants.App.AppName} {MetadataHelper.AppVersion} ({Constants.Link.GithubUrl})"
            );
            _amllTtmlDbHttpClient = new();
        }

        private static bool IsAmllTtmlDbIndexInvalid()
        {
            bool existed = File.Exists(PathHelper.AmllTtmlDbIndexPath);

            if (!existed)
            {
                return true;
            }
            else
            {
                long currentTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string lastUpdatedStr = File.ReadAllText(PathHelper.AmllTtmlDbLastUpdatedPath);
                long lastUpdated = Convert.ToInt64(lastUpdatedStr);
                return currentTs - lastUpdated > 1 * 24 * 60 * 60;
            }
        }

        public async Task<bool> DownloadAmllTtmlDbIndexAsync()
        {
            try
            {
                using var response = await _amllTtmlDbHttpClient.GetAsync(Constants.AmllTTmlDB.Index, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode) return false;

                await using var stream = await response.Content.ReadAsStreamAsync();
                await using var fs = new FileStream(
                    PathHelper.AmllTtmlDbIndexPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None
                );
                await stream.CopyToAsync(fs);

                long currentTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                File.WriteAllText(PathHelper.AmllTtmlDbLastUpdatedPath, currentTs.ToString());

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<(string?, LyricsSearchProvider?)> SearchAsync(string mediaSessionId, string title, string artist, string album, double durationMs, CancellationToken token)
        {
            _logger.LogInformation("Searching img for: {Title} - {Artist} (Album: {Album}, Duration: {DurationMs}ms)", title, artist, album, durationMs);

            try
            {
                foreach (var provider in _settingsService.AppSettings.MediaSourceProvidersInfo.Where(x => x.Provider == mediaSessionId).FirstOrDefault()?.LyricsSearchProvidersInfo ?? [])
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
                        cachedLyrics = FileHelper.ReadLyricsCache(title, artist, lyricsFormat, provider.Provider.GetCacheDirectory());
                        if (!string.IsNullOrWhiteSpace(cachedLyrics))
                        {
                            return (cachedLyrics, provider.Provider);
                        }
                    }

                    string? searchedLyrics = null;

                    if (provider.Provider.IsLocal())
                    {
                        if (provider.Provider == LyricsSearchProvider.LocalMusicFile)
                        {
                            searchedLyrics = SearchEmbedded(title, artist);
                        }
                        else
                        {
                            searchedLyrics = await SearchFile(title, artist, lyricsFormat);
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
                                searchedLyrics = await SearchQQNeteaseKugouAsync(title, artist, album, (int)durationMs, Searchers.QQMusic);
                                break;
                            case LyricsSearchProvider.Kugou:
                                searchedLyrics = await SearchQQNeteaseKugouAsync(title, artist, album, (int)durationMs, Searchers.Kugou);
                                break;
                            case LyricsSearchProvider.Netease:
                                searchedLyrics = await SearchQQNeteaseKugouAsync(title, artist, album, (int)durationMs, Searchers.Netease);
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
                            FileHelper.WriteLyricsCache(title, artist, searchedLyrics, lyricsFormat, provider.Provider.GetCacheDirectory());
                        }

                        return (searchedLyrics, provider.Provider);
                    }
                }
            }
            catch (Exception) { }

            return (null, null);
        }

        private async Task<string?> SearchFile(string title, string artist, LyricsFormat format)
        {
            foreach (var folder in _settingsService.AppSettings.LocalMediaFolders)
            {
                if (Directory.Exists(folder.Path) && folder.IsEnabled)
                {
                    foreach (var file in Directory.GetFiles(folder.Path, $"*{format.ToFileExtension()}", SearchOption.AllDirectories))
                    {
                        if (FileHelper.IsSwitchableNormalizedMatch(Path.GetFileNameWithoutExtension(file), title, artist))
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

        private string? SearchEmbedded(string title, string artist)
        {
            foreach (var folder in _settingsService.AppSettings.LocalMediaFolders)
            {
                if (Directory.Exists(folder.Path) && folder.IsEnabled)
                {
                    foreach (var file in Directory.GetFiles(folder.Path, $"*.*", SearchOption.AllDirectories))
                    {
                        var track = new Track(file);
                        if ((track.Title == title && track.Artist == artist) || FileHelper.IsSwitchableNormalizedMatch(Path.GetFileNameWithoutExtension(file), title, artist))
                        {
                            try
                            {
                                var plain = TagLib.File.Create(file).Tag.Lyrics;
                                if (!plain.IsNullOrEmpty())
                                {
                                    return plain;
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }
            return null;
        }

        private async Task<string?> SearchAmllTtmlDbAsync(string title, string artist)
        {
            if (IsAmllTtmlDbIndexInvalid())
            {
                var downloadOk = await DownloadAmllTtmlDbIndexAsync();
                if (!downloadOk)
                    return null;
            }

            string? rawLyricFile = null;
            await foreach (var line in File.ReadLinesAsync(PathHelper.AmllTtmlDbIndexPath))
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

                    if (FileHelper.IsSwitchableNormalizedMatch($"{artists} - {musicName}", title, artist))
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
            var url = $"{Constants.AmllTTmlDB.QueryPrefix}{rawLyricFile}";
            try
            {
                using var response = await _amllTtmlDbHttpClient.GetAsync(url);
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

            using var response = await _lrcLibHttpClient.GetAsync(url);
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

        private static async Task<string?> SearchQQNeteaseKugouAsync(string title, string artist, string album, int durationMs, Searchers searchers)
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
                var translated = response?.Trans;
                if (!string.IsNullOrEmpty(translated))
                {
                    FileHelper.WriteLyricsCache(
                        title,
                        artist,
                        translated,
                        LyricsFormat.Lrc,
                        PathHelper.QQTranslationCacheDirectory
                    );
                }
                return original;
            }
            else if (result is NeteaseSearchResult neteaseResult)
            {
                var response = await Lyricify.Lyrics.Helpers.ProviderHelper.NeteaseApi.GetLyric(neteaseResult.Id);
                var translated = response?.Tlyric?.Lyric;
                if (!string.IsNullOrEmpty(translated))
                {
                    FileHelper.WriteLyricsCache(
                        title,
                        artist,
                        translated,
                        LyricsFormat.Lrc,
                        PathHelper.NeteaseTranslationCacheDirectory
                    );
                }
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
    }
}
