// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Providers;
using BetterLyrics.WinUI3.Services.FileSystemService;
using BetterLyrics.WinUI3.Services.SettingsService;
using Lyricify.Lyrics.Helpers;
using Lyricify.Lyrics.Searchers;
using Microsoft.Extensions.Logging;
using NTextCat.Commons;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.LyricsSearchService
{
    public class LyricsSearchService : ILyricsSearchService
    {
        private readonly HttpClient _amllTtmlDbHttpClient;
        private readonly HttpClient _lrcLibHttpClient;
        private readonly AppleMusic _appleMusic;

        private readonly ISettingsService _settingsService;
        private readonly IFileSystemService _fileSystemService;
        private readonly ILogger _logger;

        public LyricsSearchService(ISettingsService settingsService, IFileSystemService fileSystemService, ILogger<LyricsSearchService> logger)
        {
            _settingsService = settingsService;
            _fileSystemService = fileSystemService;
            _logger = logger;

            _lrcLibHttpClient = new();
            _lrcLibHttpClient.DefaultRequestHeaders.Add(
                "User-Agent",
                $"{Constants.App.AppName} {MetadataHelper.AppVersion} ({Constants.Link.BetterLyricsGitHub})"
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
                using var response = await _amllTtmlDbHttpClient.GetAsync($"{_settingsService.AppSettings.GeneralSettings.AmllTtmlDbBaseUrl}/{Constants.AmllTTmlDB.IndexSuffix}", HttpCompletionOption.ResponseHeadersRead);
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

        public async Task<LyricsSearchResult?> SearchSmartlyAsync(SongInfo songInfo, bool checkCache, LyricsSearchType? lyricsSearchType, CancellationToken token)
        {
            if (lyricsSearchType == null)
            {
                return null;
            }

            var lyricsSearchResult = new LyricsSearchResult();

            string overridenTitle = songInfo.Title;
            string[] overridenArtists = songInfo.Artists;
            string overridenAlbum = songInfo.Album;

            _logger.LogInformation("SearchSmartlyAsync {SongInfo}", songInfo);

            // 先检查该曲目是否已被用户映射
            var found = _settingsService.AppSettings.MappedSongSearchQueries
                .FirstOrDefault(x =>
                    x.OriginalTitle == overridenTitle &&
                    x.OriginalArtist == overridenArtists.Join(ATL.Settings.DisplayValueSeparator.ToString()) &&
                    x.OriginalAlbum == overridenAlbum);

            if (found != null)
            {
                overridenTitle = found.MappedTitle;
                overridenArtists = found.MappedArtist.Split(ATL.Settings.DisplayValueSeparator);
                overridenAlbum = found.MappedAlbum;

                _logger.LogInformation("Found mapped song search query: {MappedSongSearchQuery}", found);

                var pureMusic = found.IsMarkedAsPureMusic;
                if (pureMusic)
                {
                    lyricsSearchResult.Title = overridenTitle;
                    lyricsSearchResult.Artists = overridenArtists;
                    lyricsSearchResult.Album = overridenAlbum;
                    lyricsSearchResult.Raw = "[99:00.000]🎶🎶🎶";
                    return lyricsSearchResult;
                }

                var targetProvider = found.LyricsSearchProvider;
                if (targetProvider != null)
                {
                    return await SearchSingleAsync(
                        ((SongInfo)songInfo.Clone())
                            .WithTitle(overridenTitle)
                            .WithArtist(overridenArtists)
                            .WithAlbum(overridenAlbum),
                        targetProvider.Value, checkCache, token);
                }
            }

            List<LyricsSearchResult> lyricsSearchResults = [];

            var mediaSourceProviderInfo = _settingsService.AppSettings.MediaSourceProvidersInfo.FirstOrDefault(x => x.Provider == songInfo.PlayerId);
            if (mediaSourceProviderInfo != null)
            {
                // 曲目没有被映射
                foreach (var provider in mediaSourceProviderInfo.LyricsSearchProvidersInfo)
                {
                    if (!provider.IsEnabled)
                    {
                        continue;
                    }

                    lyricsSearchResult = await SearchSingleAsync(
                        ((SongInfo)songInfo.Clone())
                            .WithTitle(overridenTitle)
                            .WithArtist(overridenArtists)
                            .WithAlbum(overridenAlbum),
                        provider.Provider, checkCache, token);

                    int matchingThreshold = mediaSourceProviderInfo.MatchingThreshold;
                    if (provider.IsMatchingThresholdOverwritten)
                    {
                        matchingThreshold = provider.MatchingThreshold;
                    }

                    if (lyricsSearchResult.IsFound && lyricsSearchResult.MatchPercentage >= matchingThreshold)
                    {
                        switch (lyricsSearchType)
                        {
                            case LyricsSearchType.Sequential:
                                return lyricsSearchResult;
                            case LyricsSearchType.BestMatch:
                                lyricsSearchResults.Add((LyricsSearchResult)lyricsSearchResult.Clone());
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            return lyricsSearchType switch
            {
                LyricsSearchType.Sequential => lyricsSearchResult,
                LyricsSearchType.BestMatch => lyricsSearchResults.OrderByDescending(x => x.MatchPercentage).FirstOrDefault(),
                _ => null,
            };
        }

        public async Task<List<LyricsSearchResult>> SearchAllAsync(SongInfo songInfo, bool checkCache, CancellationToken token)
        {
            _logger.LogInformation("SearchAllAsync {SongInfo}", songInfo);
            var results = new List<LyricsSearchResult>();
            foreach (var provider in Enum.GetValues<LyricsSearchProvider>())
            {
                var searchResult = await SearchSingleAsync(songInfo, provider, checkCache, token);
                results.Add(searchResult);
            }
            return results;
        }

        private async Task<LyricsSearchResult> SearchSingleAsync(SongInfo songInfo, LyricsSearchProvider provider, bool checkCache, CancellationToken token)
        {
            var lyricsSearchResult = new LyricsSearchResult
            {
                Provider = provider,
            };

            try
            {
                // Check cache first if allowed
                if (checkCache && provider.IsRemote())
                {
                    var cached = FileHelper.ReadLyricsCache(songInfo, provider);
                    if (cached != null)
                    {
                        lyricsSearchResult = cached;
                        return lyricsSearchResult;
                    }
                }

                switch (provider)
                {
                    case LyricsSearchProvider.QQ:
                        lyricsSearchResult = await SearchQQNeteaseKugouAsync(songInfo, Searchers.QQMusic);
                        break;
                    case LyricsSearchProvider.Kugou:
                        lyricsSearchResult = await SearchQQNeteaseKugouAsync(songInfo, Searchers.Kugou);
                        break;
                    case LyricsSearchProvider.Netease:
                        lyricsSearchResult = await SearchQQNeteaseKugouAsync(songInfo, Searchers.Netease);
                        break;
                    case LyricsSearchProvider.LrcLib:
                        lyricsSearchResult = await SearchLrcLibAsync(songInfo);
                        break;
                    case LyricsSearchProvider.AmllTtmlDb:
                        lyricsSearchResult = await SearchAmllTtmlDbAsync(songInfo);
                        break;
                    case LyricsSearchProvider.LocalMusicFile:
                        lyricsSearchResult = await SearchEmbedded(songInfo);
                        break;
                    case LyricsSearchProvider.LocalLrcFile:
                    case LyricsSearchProvider.LocalEslrcFile:
                    case LyricsSearchProvider.LocalTtmlFile:
                        lyricsSearchResult = await SearchFile(songInfo, provider.GetLyricsFormat());
                        break;
                    case LyricsSearchProvider.AppleMusic:
                        lyricsSearchResult = await SearchAppleMusicAsync(songInfo);
                        break;
                    default:
                        break;
                }

                if (token.IsCancellationRequested)
                {
                    return lyricsSearchResult;
                }

            }
            catch (Exception)
            {
            }

            if (provider.IsRemote())
            {
                FileHelper.WriteLyricsCache(songInfo, lyricsSearchResult);
            }

            return lyricsSearchResult;
        }

        private async Task<LyricsSearchResult> SearchFile(SongInfo songInfo, LyricsFormat format)
        {
            int maxScore = 0;

            FileCacheEntity? bestFileEntity = null;
            MediaFolder? bestFolderConfig = null;

            var lyricsSearchResult = new LyricsSearchResult();
            if (format.ToLyricsSearchProvider() is LyricsSearchProvider lyricsSearchProvider)
            {
                lyricsSearchResult.Provider = lyricsSearchProvider;
            }

            string targetExt = format.ToFileExtension();

            var enabledFolders = _settingsService.AppSettings.LocalMediaFolders
                .Where(f => f.IsEnabled)
                .ToList();

            var enabledIds = enabledFolders.Select(f => f.Id).ToList();

            if (enabledIds.Count == 0) return lyricsSearchResult;

            var allFiles = await _fileSystemService.GetParsedFilesAsync(enabledIds);
            allFiles = allFiles.Where(x => FileHelper.LyricExtensions.Contains(Path.GetExtension(x.FileName))).ToList();

            foreach (var item in allFiles)
            {
                if (item.FileName.EndsWith(targetExt, StringComparison.OrdinalIgnoreCase))
                {
                    int score = MetadataComparer.CalculateScore(songInfo, new LyricsSearchResult { Reference = item.FileName });

                    if (score > maxScore)
                    {
                        maxScore = score;
                        bestFileEntity = item;

                        bestFolderConfig = enabledFolders.FirstOrDefault(f => f.Id == item.MediaFolderId);
                    }
                }
            }

            if (bestFileEntity != null)
            {
                lyricsSearchResult.Raw = bestFileEntity.EmbeddedLyrics;

                lyricsSearchResult.Reference = bestFileEntity.Uri;
                lyricsSearchResult.MatchPercentage = maxScore;
            }

            return lyricsSearchResult;
        }

        private async Task<LyricsSearchResult> SearchEmbedded(SongInfo songInfo)
        {
            var lyricsSearchResult = new LyricsSearchResult
            {
                Provider = LyricsSearchProvider.LocalMusicFile,
            };

            var enabledIds = _settingsService.AppSettings.LocalMediaFolders
                .Where(f => f.IsEnabled)
                .Select(f => f.Id)
                .ToList();

            if (enabledIds.Count == 0) return lyricsSearchResult;

            var allFiles = await _fileSystemService.GetParsedFilesAsync(enabledIds);
            allFiles = allFiles.Where(x => FileHelper.MusicExtensions.Contains(Path.GetExtension(x.FileName))).ToList();

            FileCacheEntity? bestFile = null;
            int maxScore = 0;

            foreach (var item in allFiles)
            {
                if (string.IsNullOrEmpty(item.EmbeddedLyrics)) continue;

                int score = MetadataComparer.CalculateScore(songInfo, new LyricsSearchResult
                {
                    Title = item.Title,
                    Artists = item.Artists?.Split(ATL.Settings.DisplayValueSeparator),
                    Album = item.Album,
                    Duration = item.Duration
                });

                if (score > maxScore)
                {
                    maxScore = score;
                    bestFile = item;
                }
            }

            if (bestFile != null && maxScore > 0)
            {
                lyricsSearchResult.Title = bestFile.Title;
                lyricsSearchResult.Artists = bestFile.Artists?.Split(ATL.Settings.DisplayValueSeparator);
                lyricsSearchResult.Album = bestFile.Album;
                lyricsSearchResult.Duration = bestFile.Duration;

                lyricsSearchResult.Raw = bestFile.EmbeddedLyrics;
                lyricsSearchResult.Reference = bestFile.Uri;
                lyricsSearchResult.MatchPercentage = maxScore;
            }

            return lyricsSearchResult;
        }

        private async Task<LyricsSearchResult> SearchAmllTtmlDbAsync(SongInfo songInfo)
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

            int bestScore = 0;
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

                    string? title = null;
                    string[]? artists = null;
                    string? album = null;

                    foreach (var meta in metadataArr.EnumerateArray())
                    {
                        if (meta.GetArrayLength() != 2)
                            continue;
                        var key = meta[0].GetString();
                        var valueArr = meta[1];
                        if (key == "musicName" && valueArr.GetArrayLength() > 0)
                            title = valueArr[0].GetString();
                        if (key == "artists" && valueArr.GetArrayLength() > 0)
                            artists = valueArr.EnumerateArray().Select(x => x.GetString() ?? "").ToArray();
                        if (key == "album" && valueArr.GetArrayLength() > 0)
                            album = valueArr[0].GetString();
                    }

                    int score = MetadataComparer.CalculateScore(songInfo, new LyricsSearchResult
                    {
                        Title = title,
                        Artists = artists,
                        Album = album,
                    });
                    if (score > bestScore)
                    {
                        if (root.TryGetProperty("rawLyricFile", out var rawLyricFileProp))
                        {
                            rawLyricFile = rawLyricFileProp.GetString();
                            lyricsSearchResult.Title = title;
                            lyricsSearchResult.Artists = artists;
                            lyricsSearchResult.Album = album;
                            bestScore = score;
                        }
                    }
                }
                catch { }
            }

            lyricsSearchResult.MatchPercentage = MetadataComparer.CalculateScore(songInfo, lyricsSearchResult);

            if (string.IsNullOrWhiteSpace(rawLyricFile))
            {
                return lyricsSearchResult;
            }

            // 下载歌词内容
            var url = $"{_settingsService.AppSettings.GeneralSettings.AmllTtmlDbBaseUrl}/{Constants.AmllTTmlDB.QueryPrefix}/{rawLyricFile}";
            lyricsSearchResult.Reference = url;
            try
            {
                using var response = await _amllTtmlDbHttpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return lyricsSearchResult;
                }
                string lyrics = await response.Content.ReadAsStringAsync();

                lyricsSearchResult.Raw = lyrics;
            }
            catch
            {
            }

            return lyricsSearchResult;
        }

        private async Task<LyricsSearchResult> SearchLrcLibAsync(SongInfo songInfo)
        {
            var lyricsSearchResult = new LyricsSearchResult
            {
                Provider = LyricsSearchProvider.LrcLib,
            };

            // Build API query URL
            var url =
                $"https://lrclib.net/api/search?" +
                $"track_name={Uri.EscapeDataString(songInfo.Title)}&" +
                $"artist_name={Uri.EscapeDataString(songInfo.DisplayArtists)}&" +
                $"&album_name={Uri.EscapeDataString(songInfo.Album)}" +
                $"&durationMs={Uri.EscapeDataString(songInfo.DurationMs.ToString())}";

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
            string? searchedAlbum = null;
            double? searchedDuration = null;

            if (jArr.ValueKind == JsonValueKind.Array && jArr.GetArrayLength() > 0)
            {
                var first = jArr[0];
                original = first.GetProperty("syncedLyrics").GetString();
                searchedTitle = first.GetProperty("trackName").GetString();
                searchedArtist = first.GetProperty("artistName").GetString();
                searchedAlbum = first.GetProperty("albumName").GetString();
                searchedDuration = first.GetProperty("duration").GetDouble();
            }

            lyricsSearchResult.Raw = original;
            lyricsSearchResult.Title = searchedTitle;
            lyricsSearchResult.Artists = searchedArtist?.SplitByCommonSplitter();
            lyricsSearchResult.Album = searchedAlbum;
            lyricsSearchResult.Duration = searchedDuration;

            lyricsSearchResult.Reference = url;

            lyricsSearchResult.MatchPercentage = MetadataComparer.CalculateScore(songInfo, lyricsSearchResult);

            return lyricsSearchResult;
        }

        private static async Task<LyricsSearchResult> SearchQQNeteaseKugouAsync(SongInfo songInfo, Searchers searcher)
        {
            var lyricsSearchResult = new LyricsSearchResult();

            switch (searcher)
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

            ISearchResult? result;

            if (songInfo.SongId != null && searcher == Searchers.Netease && PlayerIDHelper.IsNeteaseFamily(songInfo.PlayerId))
            {
                result = new NeteaseSearchResult(songInfo.Title, songInfo.Artists, songInfo.Album, [], (int)songInfo.DurationMs, songInfo.SongId);
            }
            else if (songInfo.SongId != null && searcher == Searchers.QQMusic && songInfo.PlayerId == Constants.PlayerID.QQMusic)
            {
                result = new QQMusicSearchResult(songInfo.Title, songInfo.Artists, songInfo.Album, [], (int)songInfo.DurationMs, songInfo.SongId, "");
            }
            else
            {
                result = await SearchHelper.Search(new Lyricify.Lyrics.Models.TrackMultiArtistMetadata()
                {
                    DurationMs = (int)songInfo.DurationMs,
                    Album = songInfo.Album,
                    AlbumArtists = songInfo.Artists.ToList(),
                    Artists = songInfo.Artists.ToList(),
                    Title = songInfo.Title,
                }, searcher, Lyricify.Lyrics.Searchers.Helpers.CompareHelper.MatchType.NoMatch);
            }

            if (result != null)
            {
                if (result is QQMusicSearchResult qqResult)
                {
                    var response = await Lyricify.Lyrics.Helpers.ProviderHelper.QQMusicApi.GetLyricsAsync(qqResult.Id);

                    lyricsSearchResult.Raw = response?.Lyrics;
                    lyricsSearchResult.Translation = response?.Trans;
                    lyricsSearchResult.Reference = $"https://y.qq.com/n/ryqq/songDetail/{qqResult.Mid}";
                }
                else if (result is NeteaseSearchResult neteaseResult)
                {
                    var response = await Lyricify.Lyrics.Helpers.ProviderHelper.NeteaseApi.GetLyric(neteaseResult.Id);

                    lyricsSearchResult.Raw = response?.Lrc?.Lyric;
                    lyricsSearchResult.Translation = response?.Tlyric?.Lyric;
                    lyricsSearchResult.Transliteration = response?.Romalrc?.Lyric;
                    lyricsSearchResult.Reference = $"https://music.163.com/song?id={neteaseResult.Id}";
                }
                else if (result is KugouSearchResult kugouResult)
                {
                    var response = await Lyricify.Lyrics.Helpers.ProviderHelper.KugouApi.GetSearchLyrics(hash: kugouResult.Hash);
                    string? original = null;
                    string? translated = null;
                    var candidate = response?.Candidates.FirstOrDefault();
                    if (candidate != null)
                    {
                        original = await Lyricify.Lyrics.Decrypter.Krc.Helper.GetLyricsAsync(candidate.Id, candidate.AccessKey);
                        if (original != null)
                        {
                            var parsedList = Lyricify.Lyrics.Parsers.KrcParser.ParseLyrics(original);
                            if (parsedList != null)
                            {
                                translated = "";
                                foreach (var item in parsedList)
                                {
                                    if (item is Lyricify.Lyrics.Models.FullSyllableLineInfo fullSyllableLineInfo)
                                    {
                                        var startTimeSpan = TimeSpan.FromMilliseconds(fullSyllableLineInfo.StartTime ?? 0);
                                        string startTimeStr = startTimeSpan.ToString(@"mm\:ss\.ff");
                                        string chTranslation = fullSyllableLineInfo.Translations.GetValueOrDefault("zh") ?? "";
                                        translated += $"[{startTimeStr}]{chTranslation}\n";
                                    }
                                }
                            }
                        }
                        lyricsSearchResult.Reference = $"https://www.kugou.com/";
                    }

                    lyricsSearchResult.Raw = original;
                    lyricsSearchResult.Translation = translated;
                }
            }

            lyricsSearchResult.Title = result?.Title;
            lyricsSearchResult.Artists = result?.Artists;
            lyricsSearchResult.Album = result?.Album;
            lyricsSearchResult.Duration = result?.DurationMs / 1000;

            lyricsSearchResult.MatchPercentage = MetadataComparer.CalculateScore(songInfo, lyricsSearchResult);

            return lyricsSearchResult;
        }

        private async Task<LyricsSearchResult> SearchAppleMusicAsync(SongInfo songInfo)
        {
            LyricsSearchResult lyricsSearchResult = new()
            {
                Provider = LyricsSearchProvider.AppleMusic
            };

            _logger.LogInformation("SearchAppleMusicAsync");

            if (await _appleMusic.InitAsync())
            {
                lyricsSearchResult = await _appleMusic.SearchSongInfoAsync(songInfo);
            }

            return lyricsSearchResult;
        }
    }
}
