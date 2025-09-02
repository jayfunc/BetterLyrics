// 2025/6/23 by Zhe Fang

using ATL;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Helper.BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.DependencyInjection;
using Lyricify.Lyrics.Providers.Web.Kugou;
using Lyricify.Lyrics.Searchers;
using Microsoft.Extensions.Logging;
using NTextCat.Commons;
using System;
using System.Collections.Generic;
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
        private readonly AppleMusic _appleMusic;

        private readonly ISettingsService _settingsService;
        private readonly ILogger _logger;

        public LyricsSearchService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _logger = Ioc.Default.GetRequiredService<ILogger<LyricsSearchService>>();

            _lrcLibHttpClient = new();
            _lrcLibHttpClient.DefaultRequestHeaders.Add(
                "User-Agent",
                $"{Constants.App.AppName} {MetadataHelper.AppVersion} ({Constants.Link.GitHubUrl})"
            );
            _amllTtmlDbHttpClient = new();
            _appleMusic = new AppleMusic();
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

        public async Task<LyricsSearchResult> SearchSmartlyAsync(string mediaSessionId, string title, string artist, string album, double durationMs, CancellationToken token)
        {
            var lyricsSearchResult = new LyricsSearchResult();

            string overridenTitle = title;
            string overridenArtist = artist;

            _logger.LogInformation("Searching img for: {Title} - {Artist} (Album: {Album}, Duration: {DurationMs}ms)", title, artist, album, durationMs);

            var found = _settingsService.AppSettings.MappedSongSearchQueries
                .Where(x => x.OriginalTitle == title && x.OriginalArtist == artist)
                .FirstOrDefault();

            if (found != null)
            {
                overridenTitle = found.MappedTitle;
                overridenArtist = found.MappedArtist;

                _logger.LogInformation("Found mapped song search query: {MappedSongSearchQuery}", found);

                var pureMusic = found.IsMarkedAsPureMusic;
                if (pureMusic)
                {
                    lyricsSearchResult.Title = overridenTitle;
                    lyricsSearchResult.Artist = overridenArtist;
                    lyricsSearchResult.Raw = "[99:00.000]🎶🎶🎶";
                    return lyricsSearchResult;
                }

                var targetProvider = found.LyricsSearchProvider;
                if (targetProvider != null)
                {
                    return await SearchSingleAsync(targetProvider.Value, overridenTitle, overridenArtist, album, durationMs, token);
                }
            }

            foreach (var provider in _settingsService.AppSettings.MediaSourceProvidersInfo.Where(x => x.Provider == mediaSessionId).FirstOrDefault()?.LyricsSearchProvidersInfo ?? [])
            {
                if (!provider.IsEnabled)
                {
                    continue;
                }

                lyricsSearchResult = await SearchSingleAsync(provider.Provider, overridenTitle, overridenArtist, album, durationMs, token);

                if (lyricsSearchResult.IsFound)
                {
                    return lyricsSearchResult;
                }
            }

            return lyricsSearchResult;
        }

        public async Task<List<LyricsSearchResult>> SearchAllAsync(string title, string artist, string album, double durationMs, CancellationToken token)
        {
            _logger.LogInformation("Searching all lyrics for: {Title} - {Artist} (Album: {Album}, Duration: {DurationMs}ms)", title, artist, album, durationMs);
            var results = new List<LyricsSearchResult>();
            foreach (var provider in Enum.GetValues<LyricsSearchProvider>())
            {
                var searchResult = await SearchSingleAsync(provider, title, artist, album, durationMs, token);
                results.Add(searchResult);
            }
            return results;
        }

        private async Task<LyricsSearchResult> SearchSingleAsync(LyricsSearchProvider provider, string title, string artist, string album, double durationMs, CancellationToken token)
        {
            var lyricsSearchResult = new LyricsSearchResult
            {
                Provider = provider,
            };

            try
            {
                LyricsFormat lyricsFormat = provider.GetLyricsFormat();

                // Check cache first
                if (provider.IsRemote())
                {
                    var cachedLyrics = FileHelper.ReadLyricsCache(title, artist, lyricsFormat, provider.GetCacheDirectory());
                    if (!string.IsNullOrWhiteSpace(cachedLyrics))
                    {
                        lyricsSearchResult.Raw = cachedLyrics;
                        lyricsSearchResult.Title = title;
                        lyricsSearchResult.Artist = artist;
                        return lyricsSearchResult;
                    }
                }

                if (provider.IsLocal())
                {
                    if (provider == LyricsSearchProvider.LocalMusicFile)
                    {
                        lyricsSearchResult = SearchEmbedded(title, artist);
                    }
                    else
                    {
                        lyricsSearchResult = await SearchFile(title, artist, lyricsFormat);
                    }
                }
                else
                {
                    switch (provider)
                    {
                        case LyricsSearchProvider.LrcLib:
                            lyricsSearchResult = await SearchLrcLibAsync(title, artist, album, (int)(durationMs / 1000));
                            break;
                        case LyricsSearchProvider.QQ:
                            lyricsSearchResult = await SearchQQNeteaseKugouAsync(title, artist, album, (int)durationMs, Searchers.QQMusic);
                            break;
                        case LyricsSearchProvider.Kugou:
                            lyricsSearchResult = await SearchQQNeteaseKugouAsync(title, artist, album, (int)durationMs, Searchers.Kugou);
                            break;
                        case LyricsSearchProvider.Netease:
                            lyricsSearchResult = await SearchQQNeteaseKugouAsync(title, artist, album, (int)durationMs, Searchers.Netease);
                            break;
                        case LyricsSearchProvider.AmllTtmlDb:
                            lyricsSearchResult = await SearchAmllTtmlDbAsync(title, artist);
                            break;
                        case LyricsSearchProvider.AppleMusic:
                            lyricsSearchResult = await SearchAppleMusicAsync(title, artist, album, (int)durationMs);
                            break;
                        default:
                            break;
                    }
                }

                if (token.IsCancellationRequested)
                {
                    return lyricsSearchResult;
                }

                if (lyricsSearchResult.IsFound)
                {
                    if (provider.IsRemote())
                    {
                        FileHelper.WriteLyricsCache(title, artist, lyricsSearchResult.Raw!, lyricsFormat, provider.GetCacheDirectory());
                    }
                }
            }
            catch (Exception)
            {
            }

            return lyricsSearchResult;
        }

        private async Task<LyricsSearchResult> SearchFile(string title, string artist, LyricsFormat format)
        {
            var lyricsSearchResult = new LyricsSearchResult
            {
                Provider = format.ToLyricsSearchProvider(),
            };

            foreach (var folder in _settingsService.AppSettings.LocalMediaFolders)
            {
                if (Directory.Exists(folder.Path) && folder.IsEnabled)
                {
                    try
                    {
                        foreach (var file in DirectoryHelper.GetAllFiles(folder.Path, $"*{format.ToFileExtension()}"))
                        {
                            if (FileHelper.IsSwitchableNormalizedMatch(Path.GetFileNameWithoutExtension(file), title, artist))
                            {
                                string? raw = await File.ReadAllTextAsync(file, FileHelper.GetEncoding(file));
                                if (raw != null)
                                {
                                    lyricsSearchResult.Raw = raw;
                                    lyricsSearchResult.Title = title;
                                    lyricsSearchResult.Artist = artist;
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            return lyricsSearchResult;
        }

        private LyricsSearchResult SearchEmbedded(string title, string artist)
        {
            var lyricsSearchResult = new LyricsSearchResult
            {
                Provider = LyricsSearchProvider.LocalMusicFile,
            };

            foreach (var folder in _settingsService.AppSettings.LocalMediaFolders)
            {
                if (Directory.Exists(folder.Path) && folder.IsEnabled)
                {
                    foreach (var file in DirectoryHelper.GetAllFiles(folder.Path))
                    {
                        if (FileHelper.MusicExtensions.Contains(Path.GetExtension(file)))
                        {
                            var track = new Track(file);
                            if ((track.Title == title && track.Artist == artist) || FileHelper.IsSwitchableNormalizedMatch(Path.GetFileNameWithoutExtension(file), title, artist))
                            {
                                var plain = TagLib.File.Create(file).Tag.Lyrics;
                                if (!plain.IsNullOrEmpty())
                                {
                                    lyricsSearchResult.Raw = plain;
                                    lyricsSearchResult.Title = track.Title;
                                    lyricsSearchResult.Artist = artist;
                                }
                            }
                        }
                    }
                }
            }
            return lyricsSearchResult;
        }

        private async Task<LyricsSearchResult> SearchAmllTtmlDbAsync(string title, string artist)
        {
            var lyricsSearchResult = new LyricsSearchResult
            {
                Provider = LyricsSearchProvider.AmllTtmlDb,
            };

            if (IsAmllTtmlDbIndexInvalid())
            {
                var downloadOk = await DownloadAmllTtmlDbIndexAsync();
                if (!downloadOk)
                {
                    return lyricsSearchResult;
                }
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
            {
                return lyricsSearchResult;
            }

            // 下载歌词内容
            var url = $"{Constants.AmllTTmlDB.QueryPrefix}{rawLyricFile}";
            try
            {
                using var response = await _amllTtmlDbHttpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return lyricsSearchResult;
                }
                string lyrics = await response.Content.ReadAsStringAsync();

                lyricsSearchResult.Raw = lyrics;
                lyricsSearchResult.Title = title;
                lyricsSearchResult.Artist = artist;
            }
            catch
            {
            }

            return lyricsSearchResult;
        }

        private async Task<LyricsSearchResult> SearchLrcLibAsync(string title, string artist, string album, int duration)
        {
            var lyricsSearchResult = new LyricsSearchResult
            {
                Provider = LyricsSearchProvider.LrcLib,
            };

            // Build API query URL
            var url =
                $"https://lrclib.net/api/search?" +
                $"track_name={Uri.EscapeDataString(title)}&" +
                $"artist_name={Uri.EscapeDataString(artist)}&" +
                $"&album_name={Uri.EscapeDataString(album)}" +
                $"&durationMs={Uri.EscapeDataString(duration.ToString())}";

            using var response = await _lrcLibHttpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return lyricsSearchResult;
            }

            var json = await response.Content.ReadAsStringAsync();

            var jArr = JsonSerializer.Deserialize(
                json,
                Serialization.SourceGenerationContext.Default.JsonElement
            );

            string? original = null;
            string? searchedTitle = null;
            string? searchedArtist = null;

            if (jArr.ValueKind == JsonValueKind.Array && jArr.GetArrayLength() > 0)
            {
                var first = jArr[0];
                original = first.GetProperty("syncedLyrics").GetString();
                searchedTitle = first.GetProperty("trackName").GetString();
                searchedArtist = first.GetProperty("artistName").GetString();
            }

            lyricsSearchResult.Raw = original;
            lyricsSearchResult.Title = searchedTitle;
            lyricsSearchResult.Artist = searchedArtist;

            return lyricsSearchResult;
        }

        private static async Task<LyricsSearchResult> SearchQQNeteaseKugouAsync(string title, string artist, string album, int durationMs, Searchers searchers)
        {
            var lyricsSearchResult = new LyricsSearchResult();

            switch (searchers)
            {
                case Searchers.QQMusic:
                    lyricsSearchResult.Provider = LyricsSearchProvider.QQ;
                    break;
                case Searchers.Netease:
                    lyricsSearchResult.Provider = LyricsSearchProvider.Netease;
                    break;
                case Searchers.Kugou:
                    lyricsSearchResult.Provider = LyricsSearchProvider.Kugou;
                    break;
                case Searchers.Musixmatch:
                    break;
                default:
                    break;
            }

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

                lyricsSearchResult.Raw = original;
                lyricsSearchResult.Title = qqResult.Title;
                lyricsSearchResult.Artist = qqResult.Artists.Join(" | ");
            }
            else if (result is NeteaseSearchResult neteaseResult)
            {
                var response = await Lyricify.Lyrics.Helpers.ProviderHelper.NeteaseApi.GetLyric(neteaseResult.Id);
                var original = response?.Lrc?.Lyric;
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

                lyricsSearchResult.Raw = original;
                lyricsSearchResult.Title = neteaseResult.Title;
                lyricsSearchResult.Artist = neteaseResult.Artists.Join(" | ");
            }
            else if (result is KugouSearchResult kugouResult)
            {
                var response = await Lyricify.Lyrics.Helpers.ProviderHelper.KugouApi.GetSearchLyrics(hash: kugouResult.Hash);
                string? original = null;
                if (response?.Candidates.FirstOrDefault() is SearchLyricsResponse.Candidate candidate)
                {
                    original = await Lyricify.Lyrics.Decrypter.Krc.Helper.GetLyricsAsync(candidate.Id, candidate.AccessKey);
                    if (candidate.TransId != null)
                    {
                        string? translated = await Lyricify.Lyrics.Decrypter.Krc.Helper.GetLyricsAsync(candidate.TransId, candidate.AccessKey);
                        if (!string.IsNullOrEmpty(translated))
                        {
                            FileHelper.WriteLyricsCache(
                                title,
                                artist,
                                translated,
                                LyricsFormat.Lrc,
                                PathHelper.KugouTranslationCacheDirectory
                            );
                        }
                    }
                }

                lyricsSearchResult.Raw = original;
                lyricsSearchResult.Title = kugouResult.Title;
                lyricsSearchResult.Artist = kugouResult.Artists.Join(" | ");
            }

            return lyricsSearchResult;
        }

        private async Task<LyricsSearchResult> SearchAppleMusicAsync(string title, string artist, string album, int durationMs)
        {
            var lyricsSearchResult = new LyricsSearchResult
            {
                Provider = LyricsSearchProvider.AppleMusic,
            };

            if (await _appleMusic.InitAsync())
            {
                var raw = await _appleMusic.GetLyricsAsync(title, artist);
                _logger.LogInformation("Apple Music lyrics search result for {Title} - {Artist}: {Raw}", title, artist, raw ?? "null");
                lyricsSearchResult.Raw = raw;
                lyricsSearchResult.Title = title;
                lyricsSearchResult.Artist = artist;
            }

            return lyricsSearchResult;
        }
    }
}
