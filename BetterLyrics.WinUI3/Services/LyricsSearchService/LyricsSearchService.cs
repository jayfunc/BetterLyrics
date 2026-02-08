// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Features;
using BetterLyrics.WinUI3.Constants;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Entities;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.FileSystemService;
using BetterLyrics.WinUI3.Services.LyricsCacheService;
using BetterLyrics.WinUI3.Services.PluginService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Services.SongSearchMapService;
using Lyricify.Lyrics.Helpers;
using Lyricify.Lyrics.Searchers;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.LyricsSearchService
{
    public class LyricsSearchService : ILyricsSearchService
    {
        private readonly HttpClient _amllTtmlDbHttpClient;
        private readonly HttpClient _lrcLibHttpClient;
        private readonly Providers.AppleMusic _appleMusic;

        private readonly ISettingsService _settingsService;
        private readonly IFileSystemService _fileSystemService;
        private readonly ILyricsCacheService _lyricsCacheService;
        private readonly ISongSearchMapService _songSearchMapService;
        private readonly IPluginService _pluginService;
        private readonly ILogger _logger;

        public LyricsSearchService(
            ISettingsService settingsService,
            IFileSystemService fileSystemService,
            ILyricsCacheService lyricsCacheService,
            ISongSearchMapService songSearchMapService,
            IPluginService pluginService,
            ILogger<LyricsSearchService> logger
        )
        {
            _settingsService = settingsService;
            _fileSystemService = fileSystemService;
            _lyricsCacheService = lyricsCacheService;
            _songSearchMapService = songSearchMapService;
            _pluginService = pluginService;
            _logger = logger;

            _lrcLibHttpClient = new();
            _lrcLibHttpClient.DefaultRequestHeaders.Add(
                "User-Agent",
                $"{Constants.App.AppName} {MetadataHelper.AppVersion} ({Link.BetterLyricsGitHub})"
            );
            _amllTtmlDbHttpClient = new();
            _appleMusic = new Providers.AppleMusic();
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

        public async Task<LyricsCacheItem?> SearchSmartlyAsync(SongInfo songInfo, LyricsSearchType? lyricsSearchType, CancellationToken token)
        {
            if (lyricsSearchType == null)
            {
                return null;
            }

            var lyricsSearchResult = new LyricsCacheItem();

            string overridenTitle = songInfo.Title;
            string overridenArtist = songInfo.Artist;
            string overridenAlbum = songInfo.Album;

            _logger.LogInformation("SearchSmartlyAsync {SongInfo}", songInfo);

            // 先检查该曲目是否已被用户映射
            var found = await _songSearchMapService.GetMappingAsync(overridenTitle, overridenArtist, overridenAlbum);

            if (found != null)
            {
                overridenTitle = found.MappedTitle;
                overridenArtist = found.MappedArtist;
                overridenAlbum = found.MappedAlbum;

                _logger.LogInformation("Found mapped song search query: {MappedSongSearchQuery}", found);

                var pureMusic = found.IsMarkedAsPureMusic;
                if (pureMusic)
                {
                    lyricsSearchResult.Title = overridenTitle;
                    lyricsSearchResult.Artist = overridenArtist;
                    lyricsSearchResult.Album = overridenAlbum;
                    lyricsSearchResult.Raw = "[00:00.000]🎶🎶🎶\n[99:00.000]";
                    return lyricsSearchResult;
                }

                var targetProvider = found.LyricsSearchProvider;
                if (targetProvider != null)
                {
                    return await SearchSingleAsync(
                        ((SongInfo)songInfo.Clone())
                            .WithTitle(overridenTitle)
                            .WithArtist(overridenArtist)
                            .WithAlbum(overridenAlbum),
                        targetProvider.Value, true, token);
                }
            }

            var mediaSourceProviderInfo = _settingsService.AppSettings.MediaSourceProvidersInfo.FirstOrDefault(x => x.Provider == songInfo.PlayerId);
            if (mediaSourceProviderInfo == null) return null;

            var enabledProviders = mediaSourceProviderInfo.LyricsSearchProvidersInfo.Where(x => x.IsEnabled).ToList();
            if (enabledProviders.Count == 0) return null;

            var baseSearchInfo = ((SongInfo)songInfo.Clone())
                .WithTitle(overridenTitle)
                .WithArtist(overridenArtist)
                .WithAlbum(overridenAlbum);

            if (lyricsSearchType == LyricsSearchType.BestMatch)
            {
                var searchTasks = enabledProviders.Select(async provider =>
                {
                    if (token.IsCancellationRequested) return null;

                    var result = await SearchSingleAsync(
                        (SongInfo)baseSearchInfo.Clone(),
                        provider.Provider,
                        !provider.IgnoreCacheWhenSearching,
                        token);

                    int threshold = provider.IsMatchingThresholdOverwritten
                        ? provider.MatchingThreshold
                        : mediaSourceProviderInfo.MatchingThreshold;

                    if (result.IsFound && result.MatchPercentage >= threshold)
                    {
                        return result;
                    }
                    return null;
                });

                var allResults = await Task.WhenAll(searchTasks);

                return allResults
                    .Where(r => r != null)
                    .OrderByDescending(r => r.MatchPercentage)
                    .FirstOrDefault();
            }

            else if (lyricsSearchType == LyricsSearchType.Sequential)
            {
                foreach (var provider in enabledProviders)
                {
                    if (token.IsCancellationRequested) break;

                    var result = await SearchSingleAsync(
                        (SongInfo)baseSearchInfo.Clone(),
                        provider.Provider,
                        !provider.IgnoreCacheWhenSearching,
                        token);

                    int threshold = provider.IsMatchingThresholdOverwritten
                        ? provider.MatchingThreshold
                        : mediaSourceProviderInfo.MatchingThreshold;

                    if (result.IsFound && result.MatchPercentage >= threshold)
                    {
                        return result;
                    }
                }
            }

            return null;
        }

        public async IAsyncEnumerable<LyricsCacheItem> SearchAllAsync(
            SongInfo songInfo,
            bool checkCache,
            [EnumeratorCancellation] CancellationToken token)
        {
            _logger.LogInformation("SearchAllAsync Concurrent {SongInfo}", songInfo);

            var searchTasks = new List<Task<LyricsCacheItem>>();

            foreach (var provider in Enum.GetValues<LyricsSearchProvider>())
            {
                searchTasks.Add(SearchSingleAsync(songInfo, provider, checkCache, token));
            }

            foreach (var plugin in _settingsService.AppSettings.PluginsInfo)
            {
                if (plugin.Plugin is ILyricsSource)
                {
                    searchTasks.Add(SearchPluginAsync(songInfo, plugin, token));
                }
            }

            await foreach (var task in Task.WhenEach(searchTasks))
            {
                if (token.IsCancellationRequested) yield break;

                LyricsCacheItem? result = null;
                try
                {
                    result = await task;
                }
                catch { }

                if (result != null) yield return result;
            }
        }

        private async Task<LyricsCacheItem> SearchSingleAsync(SongInfo songInfo, LyricsSearchProvider provider, bool checkCache, CancellationToken token)
        {
            var lyricsSearchResult = new LyricsCacheItem
            {
                Provider = provider,
            };

            try
            {
                // Check cache first if allowed
                if (checkCache && provider.IsRemote())
                {
                    var cached = await _lyricsCacheService.GetLyricsAsync(songInfo, provider);
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
                        lyricsSearchResult = await SearchMusicFileAsync(songInfo);
                        break;
                    case LyricsSearchProvider.LocalLrcFile:
                    case LyricsSearchProvider.LocalEslrcFile:
                    case LyricsSearchProvider.LocalTtmlFile:
                        lyricsSearchResult = await SearchLyricsFileAsync(songInfo, provider.GetLyricsFormat());
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
                await _lyricsCacheService.SaveLyricsAsync(songInfo, lyricsSearchResult);
            }

            return lyricsSearchResult;
        }

        private async Task<LyricsCacheItem> SearchLyricsFileAsync(SongInfo songInfo, LyricsFormat format)
        {
            int maxScore = -1;

            FilesIndexItem? bestFileEntity = null;
            MediaFolder? bestFolderConfig = null;

            var lyricsSearchResult = new LyricsCacheItem();
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
                    int score = MetadataComparer.CalculateScore(songInfo, item);

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

                lyricsSearchResult.Title = bestFileEntity.Title;
                lyricsSearchResult.Artist = bestFileEntity.Artist;
                lyricsSearchResult.Album = bestFileEntity.Album;
                lyricsSearchResult.Duration = bestFileEntity.Duration;

                lyricsSearchResult.Reference = bestFileEntity.Uri;
                lyricsSearchResult.MatchPercentage = maxScore;
            }

            return lyricsSearchResult;
        }

        private async Task<LyricsCacheItem> SearchMusicFileAsync(SongInfo songInfo)
        {
            var lyricsSearchResult = new LyricsCacheItem
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

            FilesIndexItem? bestFile = null;
            int maxScore = 0;

            foreach (var item in allFiles)
            {
                if (string.IsNullOrEmpty(item.EmbeddedLyrics)) continue;

                int score = MetadataComparer.CalculateScore(songInfo, item);

                if (score > maxScore)
                {
                    maxScore = score;
                    bestFile = item;
                }
            }

            if (bestFile != null && maxScore > 0)
            {
                lyricsSearchResult.Title = bestFile.Title;
                lyricsSearchResult.Artist = bestFile.Artist;
                lyricsSearchResult.Album = bestFile.Album;
                lyricsSearchResult.Duration = bestFile.Duration;

                lyricsSearchResult.Raw = bestFile.EmbeddedLyrics;
                lyricsSearchResult.Reference = bestFile.Uri;
                lyricsSearchResult.MatchPercentage = maxScore;
            }

            return lyricsSearchResult;
        }

        private async Task<LyricsCacheItem> SearchAmllTtmlDbAsync(SongInfo songInfo)
        {
            var lyricsSearchResult = new LyricsCacheItem
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
            string? bestNcmMusicId = null;

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
                    string? artist = null;
                    string? album = null;
                    string? ncmMusicId = null;

                    foreach (var meta in metadataArr.EnumerateArray())
                    {
                        if (meta.GetArrayLength() != 2)
                            continue;
                        var key = meta[0].GetString();
                        var valueArr = meta[1];
                        if (key == "musicName" && valueArr.GetArrayLength() > 0)
                            title = valueArr[0].GetString();
                        if (key == "artists" && valueArr.GetArrayLength() > 0)
                            artist = string.Join("/", valueArr.EnumerateArray());
                        if (key == "album" && valueArr.GetArrayLength() > 0)
                            album = valueArr[0].GetString();
                        if (key == "ncmMusicId" && valueArr.GetArrayLength() > 0)
                            ncmMusicId = valueArr[0].GetString();
                    }

                    int score = MetadataComparer.CalculateScore(songInfo, new LyricsCacheItem
                    {
                        Title = title,
                        Artist = artist,
                        Album = album,
                    });
                    if (score > lyricsSearchResult.MatchPercentage)
                    {
                        if (root.TryGetProperty("rawLyricFile", out var rawLyricFileProp))
                        {
                            bestNcmMusicId = ncmMusicId;
                            rawLyricFile = rawLyricFileProp.GetString();
                            lyricsSearchResult.Title = title;
                            lyricsSearchResult.Artist = artist;
                            lyricsSearchResult.Album = album;
                            lyricsSearchResult.MatchPercentage = score;
                        }
                    }
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(rawLyricFile))
            {
                return lyricsSearchResult;
            }

            var url = $"{_settingsService.AppSettings.GeneralSettings.AmllTtmlDbBaseUrl}/{AmllTTmlDB.QueryPrefix}/{rawLyricFile}";
            lyricsSearchResult.Reference = url;
            try
            {
                // 下载写入歌词
                using var response = await _amllTtmlDbHttpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return lyricsSearchResult;
                }
                string lyrics = await response.Content.ReadAsStringAsync();
                lyricsSearchResult.Raw = lyrics;

                // 反查时长
                if (bestNcmMusicId != null && lyricsSearchResult.Duration == null)
                {
                    var tmp = await SearchQQNeteaseKugouAsync(
                        ((SongInfo)songInfo.Clone()).WithSongId($"{ExtendedGenreFiled.NetEaseCloudMusicTrackID}{bestNcmMusicId}"),
                        Searchers.Netease);
                    lyricsSearchResult.Duration = tmp.Duration;
                    lyricsSearchResult.MatchPercentage = MetadataComparer.CalculateScore(songInfo, lyricsSearchResult);
                }
            }
            catch
            {
            }

            return lyricsSearchResult;
        }

        private async Task<LyricsCacheItem> SearchLrcLibAsync(SongInfo songInfo)
        {
            var lyricsSearchResult = new LyricsCacheItem
            {
                Provider = LyricsSearchProvider.LrcLib,
            };

            // Build API query URL
            var url =
                $"https://lrclib.net/api/search?" +
                $"track_name={Uri.EscapeDataString(songInfo.Title)}&" +
                $"artist_name={Uri.EscapeDataString(songInfo.Artist)}&" +
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
            lyricsSearchResult.Artist = searchedArtist;
            lyricsSearchResult.Album = searchedAlbum;
            lyricsSearchResult.Duration = searchedDuration;

            lyricsSearchResult.Reference = url;

            lyricsSearchResult.MatchPercentage = MetadataComparer.CalculateScore(songInfo, lyricsSearchResult);

            return lyricsSearchResult;
        }

        private static async Task<LyricsCacheItem> SearchQQNeteaseKugouAsync(SongInfo songInfo, Searchers searcher)
        {
            var lyricsSearchResult = new LyricsCacheItem();

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

            if (songInfo.SongId != null && searcher == Searchers.Netease && PlayerIdHelper.IsNeteaseFamily(songInfo.PlayerId))
            {
                result = new NeteaseSearchResult(songInfo.Title, [songInfo.Artist], songInfo.Album, [], (int)songInfo.DurationMs, songInfo.SongId);
            }
            else if (songInfo.SongId != null && searcher == Searchers.QQMusic && PlayerIdHelper.IsQQFamily(songInfo.PlayerId))
            {
                result = new QQMusicSearchResult(songInfo.Title, [songInfo.Artist], songInfo.Album, [], (int)songInfo.DurationMs, songInfo.SongId, "");
            }
            else
            {
                result = await SearchHelper.Search(new Lyricify.Lyrics.Models.TrackMultiArtistMetadata()
                {
                    DurationMs = (int)songInfo.DurationMs,
                    Album = songInfo.Album,
                    Artist = songInfo.Artist,
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
            lyricsSearchResult.Artist = result?.Artist;
            lyricsSearchResult.Album = result?.Album;
            lyricsSearchResult.Duration = result?.DurationMs / 1000;

            lyricsSearchResult.MatchPercentage = MetadataComparer.CalculateScore(songInfo, lyricsSearchResult);

            return lyricsSearchResult;
        }

        private async Task<LyricsCacheItem> SearchAppleMusicAsync(SongInfo songInfo)
        {
            LyricsCacheItem lyricsSearchResult = new()
            {
                Provider = LyricsSearchProvider.AppleMusic
            };

            _logger.LogInformation("SearchAppleMusicAsync");

            //lyricsSearchResult.Title = songInfo.Title;
            //lyricsSearchResult.Artist = songInfo.Artist;
            //lyricsSearchResult.Album = songInfo.Album;
            //lyricsSearchResult.Duration = songInfo.Duration;
            //lyricsSearchResult.Raw = "<tt xmlns=\"http://www.w3.org/ns/ttml\" xmlns:itunes=\"http://music.apple.com/lyric-ttml-internal\" xmlns:ttm=\"http://www.w3.org/ns/ttml#metadata\" itunes:timing=\"Line\" xml:lang=\"ja\"><head><metadata><ttm:agent type=\"person\" xml:id=\"v1\"><ttm:name type=\"full\">AZU</ttm:name></ttm:agent><iTunesMetadata xmlns=\"http://music.apple.com/lyric-ttml-internal\" leadingSilence=\"0.000\"><translations/><songwriters><songwriter>Naho</songwriter><songwriter>h-wonder</songwriter></songwriters></iTunesMetadata></metadata></head><body dur=\"04:06.293\"><div begin=\"00:00\" end=\"00:38.693\" itunes:songPart=\"Verse\"><p begin=\"00:00\" end=\"00:25.275\" itunes:key=\"L1\" ttm:agent=\"v1\">昨日までの痛みは和らいで 君の夢包まれる</p><p begin=\"00:25.275\" end=\"00:33.24\" itunes:key=\"L2\" ttm:agent=\"v1\">もう一度あの日のように 君を見つめたい</p><p begin=\"00:33.24\" end=\"00:38.693\" itunes:key=\"L3\" ttm:agent=\"v1\">今ならば I can say my truth</p></div><div begin=\"00:38.693\" end=\"00:54.427\" itunes:songPart=\"Verse\"><p begin=\"00:38.693\" end=\"00:45.119\" itunes:key=\"L4\" ttm:agent=\"v1\">Dream of your love, I'm thinking of you</p><p begin=\"00:45.278\" end=\"00:54.427\" itunes:key=\"L5\" ttm:agent=\"v1\">時よ take back あの日の二人に</p></div><div begin=\"00:54.427\" end=\"01:21.411\" itunes:songPart=\"Verse\"><p begin=\"00:54.427\" end=\"01:04.596\" itunes:key=\"L6\" ttm:agent=\"v1\">Every time I 最後の恋 きっと君以上に誰も</p><p begin=\"01:04.596\" end=\"01:13.995\" itunes:key=\"L7\" ttm:agent=\"v1\">愛せはしない sweet baby もっと早く強く</p><p begin=\"01:13.995\" end=\"01:21.411\" itunes:key=\"L8\" ttm:agent=\"v1\">この気持ちを baby 叶えてあげたかった</p></div><div begin=\"01:35.877\" end=\"02:01.499\" itunes:songPart=\"Verse\"><p begin=\"01:35.877\" end=\"01:48.581\" itunes:key=\"L9\" ttm:agent=\"v1\">思い出は遠ざかるほどまるで 映画のように色づく</p><p begin=\"01:48.581\" end=\"01:56.18\" itunes:key=\"L10\" ttm:agent=\"v1\">お互いに子供すぎたって 今ならわかる</p><p begin=\"01:56.18\" end=\"02:01.499\" itunes:key=\"L11\" ttm:agent=\"v1\">優しささえ棘を刺す</p></div><div begin=\"02:01.499\" end=\"02:17.614\" itunes:songPart=\"Verse\"><p begin=\"02:01.499\" end=\"02:08.352\" itunes:key=\"L12\" ttm:agent=\"v1\">Dream of your love, I'm feeling for you</p><p begin=\"02:08.352\" end=\"02:17.614\" itunes:key=\"L13\" ttm:agent=\"v1\">心 今も置き去りのままで</p></div><div begin=\"02:17.614\" end=\"02:45.979\" itunes:songPart=\"Verse\"><p begin=\"02:17.614\" end=\"02:28.674\" itunes:key=\"L14\" ttm:agent=\"v1\">Every time I 最後の恋 夢で会える君はいつも</p><p begin=\"02:28.674\" end=\"02:37.191\" itunes:key=\"L15\" ttm:agent=\"v1\">勇気をくれる sweet honey 振り向くよりちゃんと</p><p begin=\"02:37.191\" end=\"02:45.979\" itunes:key=\"L16\" ttm:agent=\"v1\">前を向いて baby 新しい自分になりたい</p></div><div begin=\"02:46.44\" end=\"03:09.476\" itunes:songPart=\"Verse\"><p begin=\"02:46.44\" end=\"02:51.331\" itunes:key=\"L17\" ttm:agent=\"v1\">明けてゆく 今日の空に</p><p begin=\"02:51.331\" end=\"03:03.814\" itunes:key=\"L18\" ttm:agent=\"v1\">君の夢と say goodbye 小さな光見つめている</p><p begin=\"03:03.814\" end=\"03:09.476\" itunes:key=\"L19\" ttm:agent=\"v1\">My heart is still brightly</p></div><div begin=\"03:09.476\" end=\"03:53.997\" itunes:songPart=\"Verse\"><p begin=\"03:09.476\" end=\"03:18.939\" itunes:key=\"L20\" ttm:agent=\"v1\">Every time I 最後の恋 きっと君以上に誰も</p><p begin=\"03:18.939\" end=\"03:28.685\" itunes:key=\"L21\" ttm:agent=\"v1\">愛せはしない sweet baby もっと早く強く</p><p begin=\"03:28.685\" end=\"03:36.262\" itunes:key=\"L22\" ttm:agent=\"v1\">この気持ちをbaby 叶えてあげたかった</p><p begin=\"03:36.262\" end=\"03:53.997\" itunes:key=\"L23\" ttm:agent=\"v1\">Sweet baby</p></div></body></tt>";
            //lyricsSearchResult.MatchPercentage = 100;

            if (await _appleMusic.InitAsync())
            {
                lyricsSearchResult = await _appleMusic.SearchSongInfoAsync(songInfo);
            }

            return lyricsSearchResult;
        }

        private async Task<LyricsCacheItem> SearchPluginAsync(SongInfo songInfo, PluginInfo pluginInfo, CancellationToken token)
        {
            var plugin = (ILyricsSource)pluginInfo.Plugin!;
            var cacheItem = new LyricsCacheItem
            {
                Provider = (LyricsSearchProvider)_pluginService.GetHashedId(pluginInfo.Id),
            };

            try
            {
                var result = await plugin.GetLyricsAsync(songInfo.Title, songInfo.Artist, songInfo.Album, songInfo.Duration);

                if (result != null && !string.IsNullOrEmpty(result.Raw))
                {
                    cacheItem.Title = result.Title;
                    cacheItem.Artist = result.Artist;
                    cacheItem.Album = result.Album;
                    cacheItem.Duration = result.Duration;

                    cacheItem.Raw = result.Raw;
                    cacheItem.Translation = result.Translation;
                    cacheItem.Transliteration = result.Transliteration;

                    cacheItem.Reference = result.Reference ?? "about:blank";
                    cacheItem.MatchPercentage = MetadataComparer.CalculateScore(songInfo, cacheItem);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Plugin {PluginName} failed to search", plugin);
            }

            return cacheItem;
        }

    }
}
