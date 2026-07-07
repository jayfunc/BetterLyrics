// 2025/6/23 by Zhe Fang

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.Serialization;
using BetterLyrics.Sdk.Interfaces.Plugins;
using Lyricify.Lyrics.Decrypter.Krc;
using Lyricify.Lyrics.Helpers;
using Lyricify.Lyrics.Models;
using Lyricify.Lyrics.Parsers;
using Lyricify.Lyrics.Searchers;
using Lyricify.Lyrics.Searchers.Helpers;
using Microsoft.Extensions.Logging;
using AppleMusic = BetterLyrics.Core.Implementations.Services.LyricsSearchService.Providers.AppleMusic;

namespace BetterLyrics.Core.Implementations.Services.LyricsSearchService;

public class LyricsSearchService : ILyricsSearchService
{
    private readonly HttpClient _amllTtmlDbHttpClient;
    private readonly AppleMusic _appleMusic;
    private readonly IFileSystemService _fileSystemService;
    private readonly ILogger _logger;
    private readonly HttpClient _lrcLibHttpClient;
    private readonly ILyricsCacheService _lyricsCacheService;
    private readonly IPluginService _pluginService;

    private readonly ISettingsService _settingsService;
    private readonly ISongSearchMapService _songSearchMapService;

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

        _lrcLibHttpClient = new HttpClient();
        _lrcLibHttpClient.DefaultRequestHeaders.Add(
            "User-Agent",
            $"{App.AppName} {MetadataHelper.AppVersion} ({Link.BetterLyricsGitHub})"
        );
        _amllTtmlDbHttpClient = new HttpClient();
        _appleMusic = new AppleMusic();
    }

    public async Task<LyricsCacheItem?> SearchSmartlyAsync(SongInfo songInfo, LyricsSearchType? lyricsSearchType,
        CancellationToken token)
    {
        LyricsCacheItem? finalResult = null;

        if (lyricsSearchType == null) return null;

        try
        {
            var lyricsSearchResult = new LyricsCacheItem();

            var overridenTitle = songInfo.Title;
            var overridenArtist = songInfo.Artist;
            var overridenAlbum = songInfo.Album;

            _logger.LogInformation("SearchSmartlyAsync {SongInfo}", songInfo);

            try
            {
                var found = await _songSearchMapService.TryGetMappingAsync(songInfo, token);

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
                        return await SearchSingleAsync(
                            ((SongInfo)songInfo.Clone())
                            .WithTitle(overridenTitle)
                            .WithArtist(overridenArtist)
                            .WithAlbum(overridenAlbum),
                            targetProvider.Value, true, token);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Failed to check song mapping, falling back to normal search.");
                _logger.LogWarning(ex, "Failed to check song mapping, falling back to normal search.");
            }

            var mediaSourceProviderInfo =
                _settingsService.AppSettings.MediaSourceProvidersInfo.FirstOrDefault(x =>
                    x.Provider == songInfo.PlayerId);
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
                    try
                    {
                        token.ThrowIfCancellationRequested();

                        var result = await SearchSingleAsync(
                            (SongInfo)baseSearchInfo.Clone(),
                            provider.Provider,
                            !provider.IgnoreCacheWhenSearching,
                            token);

                        var threshold = provider.IsMatchingThresholdOverwritten
                            ? provider.MatchingThreshold
                            : mediaSourceProviderInfo.MatchingThreshold;

                        if (result.IsFound && result.MatchPercentage >= threshold) return result;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Provider {Provider} failed during parallel search.", provider.Provider);
                        Debug.WriteLine($"Provider {provider.Provider} failed during parallel search.");
                        return null;
                    }

                    return null;
                });

                var allResults = await Task.WhenAll(searchTasks);

                finalResult = allResults
                    .Where(r => r != null)
                    .OrderByDescending(r => r.MatchPercentage)
                    .FirstOrDefault();
            }
            else if (lyricsSearchType == LyricsSearchType.Sequential)
            {
                foreach (var provider in enabledProviders)
                    try
                    {
                        var result = await SearchSingleAsync(
                            (SongInfo)baseSearchInfo.Clone(),
                            provider.Provider,
                            !provider.IgnoreCacheWhenSearching,
                            token);

                        var threshold = provider.IsMatchingThresholdOverwritten
                            ? provider.MatchingThreshold
                            : mediaSourceProviderInfo.MatchingThreshold;

                        if (result.IsFound && result.MatchPercentage >= threshold)
                        {
                            finalResult = result;
                            break;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Provider {Provider} failed during sequential search.",
                            provider.Provider);
                        Debug.WriteLine($"Provider {provider.Provider} failed during sequential search.");
                    }
            }

            if (finalResult == null) throw new Exception("Could't find any lyric");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred in SearchSmartlyAsync.");
            Debug.WriteLine($"An unexpected error occurred in SearchSmartlyAsync: {ex.Message}");
            throw;
        }

        return finalResult;
    }

    public async IAsyncEnumerable<LyricsCacheItem> SearchAllAsync(
        SongInfo songInfo,
        bool checkCache,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SearchAllAsync Concurrent {SongInfo}", songInfo);

        var searchTasks = new List<Task<LyricsCacheItem>>();

        foreach (var provider in Enum.GetValues<LyricsSearchProvider>())
            searchTasks.Add(SearchSingleAsync(songInfo, provider, checkCache, cancellationToken));

        foreach (var plugin in _settingsService.AppSettings.PluginsInfo)
            if (plugin.Plugin is ILyricsSource)
            {
                var provider = (LyricsSearchProvider)_pluginService.GetPluginHashedId(plugin.Plugin.Id);
                searchTasks.Add(SearchSingleAsync(songInfo, provider, checkCache, cancellationToken));
            }

        while (searchTasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(searchTasks);

            searchTasks.Remove(completedTask);

            LyricsCacheItem? result = null;
            try
            {
                result = await completedTask;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "A lyrics search provider failed or timed out.");
            }

            if (result != null) yield return result;
        }
    }

    public List<LyricsSearchProvider> GetActiveProviders()
    {
        List<LyricsSearchProvider> providers = [];

        foreach (var provider in Enum.GetValues<LyricsSearchProvider>()) providers.Add(provider);

        foreach (var plugin in _settingsService.AppSettings.PluginsInfo)
            if (plugin.Plugin is ILyricsSource)
            {
                var provider = (LyricsSearchProvider)_pluginService.GetPluginHashedId(plugin.Plugin.Id);
                providers.Add(provider);
            }

        return providers;
    }

    private static bool IsAmllTtmlDbIndexInvalid()
    {
        var existed = File.Exists(PathHelper.AmllTtmlDbIndexPath);

        if (!existed) return true;

        var currentTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var lastUpdatedStr = File.ReadAllText(PathHelper.AmllTtmlDbLastUpdatedPath);
        var lastUpdated = Convert.ToInt64(lastUpdatedStr);
        return currentTs - lastUpdated > 1 * 24 * 60 * 60;
    }

    public async Task<bool> DownloadAmllTtmlDbIndexAsync(CancellationToken token)
    {
        try
        {
            using var response = await _amllTtmlDbHttpClient.GetAsync(
                $"{_settingsService.AppSettings.GeneralSettings.AmllTtmlDbBaseUrl}/{AmllTTmlDB.IndexSuffix}",
                HttpCompletionOption.ResponseHeadersRead, token);

            if (!response.IsSuccessStatusCode) return false;

            await using var stream = await response.Content.ReadAsStreamAsync(token);

            await using var fs = new FileStream(
                PathHelper.AmllTtmlDbIndexPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None
            );
            await stream.CopyToAsync(fs, token);

            var currentTs = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            File.WriteAllText(PathHelper.AmllTtmlDbLastUpdatedPath, currentTs.ToString());

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<LyricsCacheItem> SearchSingleAsync(SongInfo songInfo, LyricsSearchProvider provider,
        bool checkCache, CancellationToken token)
    {
        var lyricsSearchResult = new LyricsCacheItem
        {
            Provider = provider
        };

        // Check cache first if allowed
        if (checkCache && provider.IsCacheable())
        {
            var cached = await _lyricsCacheService.GetLyricsAsync(songInfo, provider, token);
            if (cached != null)
            {
                lyricsSearchResult = cached;
                return lyricsSearchResult;
            }
        }

        if (provider.IsPlugin())
            lyricsSearchResult = await SearchPluginAsync(songInfo, provider, token);
        else
            switch (provider)
            {
                case LyricsSearchProvider.QQ:
                    lyricsSearchResult = await SearchQQNeteaseKugouAsync(songInfo, Searchers.QQMusic, token);
                    break;
                case LyricsSearchProvider.Kugou:
                    lyricsSearchResult = await SearchQQNeteaseKugouAsync(songInfo, Searchers.Kugou, token);
                    break;
                case LyricsSearchProvider.Netease:
                    lyricsSearchResult = await SearchQQNeteaseKugouAsync(songInfo, Searchers.Netease, token);
                    break;
                case LyricsSearchProvider.LrcLib:
                    lyricsSearchResult = await SearchLrcLibAsync(songInfo, token);
                    break;
                case LyricsSearchProvider.AmllTtmlDb:
                    lyricsSearchResult = await SearchAmllTtmlDbAsync(songInfo, token);
                    break;
                case LyricsSearchProvider.LocalMusicFile:
                    lyricsSearchResult = await SearchMusicFileAsync(songInfo, token);
                    break;
                case LyricsSearchProvider.LocalLrcFile:
                case LyricsSearchProvider.LocalEslrcFile:
                case LyricsSearchProvider.LocalTtmlFile:
                    lyricsSearchResult = await SearchLyricsFileAsync(songInfo, provider.GetLyricsFormat(), token);
                    break;
                case LyricsSearchProvider.AppleMusic:
                    lyricsSearchResult = await SearchAppleMusicAsync(songInfo, token);
                    break;
            }

        if (provider.IsCacheable()) await _lyricsCacheService.SaveLyricsAsync(songInfo, lyricsSearchResult, token);

        return lyricsSearchResult;
    }

    private async Task<LyricsCacheItem> SearchLyricsFileAsync(SongInfo songInfo, LyricsFormat format,
        CancellationToken token)
    {
        var maxScore = -1;

        FilesIndexItem? bestFileEntity = null;
        MediaFolder? bestFolderConfig = null;

        var lyricsSearchResult = new LyricsCacheItem();
        if (format.ToLyricsSearchProvider() is LyricsSearchProvider lyricsSearchProvider)
            lyricsSearchResult.Provider = lyricsSearchProvider;

        var targetExt = format.ToFileExtension();

        var enabledFolders = _settingsService.AppSettings.LocalMediaFolders
            .Where(f => f.IsEnabled)
            .ToList();

        var enabledIds = enabledFolders.Select(f => f.Id).ToList();

        if (enabledIds.Count == 0) return lyricsSearchResult;

        var allFiles = await _fileSystemService.GetParsedFilesAsync(enabledIds, token);
        allFiles = allFiles.Where(x => FileHelper.LyricExtensions.Contains(Path.GetExtension(x.FileName).ToLower()))
            .ToList();

        foreach (var item in allFiles)
            if (item.FileName.EndsWith(targetExt, StringComparison.OrdinalIgnoreCase))
            {
                var score = MetadataComparer.CalculateScore(songInfo, item);

                if (score > maxScore)
                {
                    maxScore = score;
                    bestFileEntity = item;

                    bestFolderConfig = enabledFolders.FirstOrDefault(f => f.Id == item.MediaFolderId);
                }
            }

        if (bestFileEntity != null)
        {
            lyricsSearchResult.Raw = bestFileEntity.EmbeddedLyrics;

            lyricsSearchResult.Title = string.IsNullOrEmpty(bestFileEntity.Title)
                ? bestFileEntity.FileName
                : bestFileEntity.Title;
            lyricsSearchResult.Artist = bestFileEntity.Artist;
            lyricsSearchResult.Album = bestFileEntity.Album;
            lyricsSearchResult.Duration = bestFileEntity.Duration;

            lyricsSearchResult.Reference = bestFileEntity.Uri;
            lyricsSearchResult.MatchPercentage = maxScore;
        }

        return lyricsSearchResult;
    }

    private async Task<LyricsCacheItem> SearchMusicFileAsync(SongInfo songInfo, CancellationToken token)
    {
        var lyricsSearchResult = new LyricsCacheItem
        {
            Provider = LyricsSearchProvider.LocalMusicFile
        };

        var enabledIds = _settingsService.AppSettings.LocalMediaFolders
            .Where(f => f.IsEnabled)
            .Select(f => f.Id)
            .ToList();

        if (enabledIds.Count == 0) return lyricsSearchResult;

        var allFiles = await _fileSystemService.GetParsedFilesAsync(enabledIds, token);
        allFiles = allFiles.Where(x => FileHelper.MusicExtensions.Contains(Path.GetExtension(x.FileName).ToLower()))
            .ToList();

        FilesIndexItem? bestFile = null;
        var maxScore = 0;

        foreach (var item in allFiles)
        {
            if (string.IsNullOrEmpty(item.EmbeddedLyrics)) continue;

            var score = MetadataComparer.CalculateScore(songInfo, item);

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

    private async Task<LyricsCacheItem> SearchAmllTtmlDbAsync(SongInfo songInfo, CancellationToken token)
    {
        var lyricsSearchResult = new LyricsCacheItem
        {
            Provider = LyricsSearchProvider.AmllTtmlDb
        };

        if (IsAmllTtmlDbIndexInvalid())
        {
            var downloadOk = await DownloadAmllTtmlDbIndexAsync(token);
            if (!downloadOk) return lyricsSearchResult;
        }

        string? rawLyricFile = null;
        string? bestNcmMusicId = null;

        await foreach (var line in File.ReadLinesAsync(PathHelper.AmllTtmlDbIndexPath, token))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (!root.TryGetProperty("metadata", out var metadataArr)) continue;

            string? title = null;
            string? artist = null;
            string? album = null;
            string? ncmMusicId = null;

            foreach (var meta in metadataArr.EnumerateArray())
            {
                if (meta.GetArrayLength() != 2) continue;
                var key = meta[0].GetString();
                var valueArr = meta[1];
                if (key == "musicName" && valueArr.GetArrayLength() > 0) title = valueArr[0].GetString();
                if (key == "artists" && valueArr.GetArrayLength() > 0)
                    artist = string.Join("/", valueArr.EnumerateArray());
                if (key == "album" && valueArr.GetArrayLength() > 0) album = valueArr[0].GetString();
                if (key == "ncmMusicId" && valueArr.GetArrayLength() > 0) ncmMusicId = valueArr[0].GetString();
            }

            var matchedById = ncmMusicId == songInfo.SongId && PlayerIdHelper.IsNeteaseFamily(songInfo.PlayerId);

            var score = MetadataComparer.CalculateScore(songInfo, new LyricsCacheItem
            {
                Title = title,
                Artist = artist,
                Album = album
            });
            if (matchedById || score > lyricsSearchResult.MatchPercentage)
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

                if (matchedById) break;
            }
        }

        if (string.IsNullOrWhiteSpace(rawLyricFile)) return lyricsSearchResult;

        var url =
            $"{_settingsService.AppSettings.GeneralSettings.AmllTtmlDbBaseUrl}/{AmllTTmlDB.QueryPrefix}/{rawLyricFile}";
        lyricsSearchResult.Reference = url;

        // 下载写入歌词
        using var response = await _amllTtmlDbHttpClient.GetAsync(url, token);
        if (!response.IsSuccessStatusCode) return lyricsSearchResult;
        var lyrics = await response.Content.ReadAsStringAsync(token);
        lyricsSearchResult.Raw = lyrics;

        // 反查时长
        if (bestNcmMusicId != null && lyricsSearchResult.Duration == null)
        {
            var tmp = await SearchQQNeteaseKugouAsync(
                ((SongInfo)songInfo.Clone()).WithSongId(
                    $"{ExtendedGenreFiled.NetEaseCloudMusicTrackID}{bestNcmMusicId}"),
                Searchers.Netease, token);
            lyricsSearchResult.Duration = tmp.Duration;
            lyricsSearchResult.MatchPercentage = MetadataComparer.CalculateScore(songInfo, lyricsSearchResult);
        }

        return lyricsSearchResult;
    }

    private async Task<LyricsCacheItem> SearchLrcLibAsync(SongInfo songInfo, CancellationToken token)
    {
        var lyricsSearchResult = new LyricsCacheItem
        {
            Provider = LyricsSearchProvider.LrcLib
        };

        // Build API query URL
        var url =
            $"https://lrclib.net/api/search?" +
            $"track_name={Uri.EscapeDataString(songInfo.Title)}&" +
            $"artist_name={Uri.EscapeDataString(songInfo.Artist)}&" +
            $"&album_name={Uri.EscapeDataString(songInfo.Album)}" +
            $"&durationMs={Uri.EscapeDataString(songInfo.DurationMs.ToString())}";

        using var response = await _lrcLibHttpClient.GetAsync(url, token);
        if (!response.IsSuccessStatusCode) return lyricsSearchResult;

        var json = await response.Content.ReadAsStringAsync(token);

        var jArr = JsonSerializer.Deserialize(
            json,
            SourceGenerationContext.Default.JsonElement
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

    private static async Task<LyricsCacheItem> SearchQQNeteaseKugouAsync(SongInfo songInfo, Searchers searcher,
        CancellationToken token)
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
        }

        ISearchResult? result;

        if (songInfo.SongId != null && searcher == Searchers.Netease &&
            PlayerIdHelper.IsNeteaseFamily(songInfo.PlayerId))
        {
            result = new NeteaseSearchResult(songInfo.Title, [songInfo.Artist], songInfo.Album, [],
                (int)songInfo.DurationMs, songInfo.SongId);
        }
        else if (songInfo.SongId != null && searcher == Searchers.QQMusic &&
                 PlayerIdHelper.IsQQFamily(songInfo.PlayerId))
        {
            result = new QQMusicSearchResult(songInfo.Title, [songInfo.Artist], songInfo.Album, [],
                (int)songInfo.DurationMs, songInfo.SongId, "");
        }
        else
        {
            result = await SearchHelper.Search(new TrackMultiArtistMetadata
            {
                DurationMs = (int)songInfo.DurationMs,
                Album = songInfo.Album,
                Artist = songInfo.Artist,
                Title = songInfo.Title
            }, searcher, CompareHelper.MatchType.NoMatch);
            token.ThrowIfCancellationRequested();
        }

        if (result != null)
        {
            if (result is QQMusicSearchResult qqResult)
            {
                var response = await ProviderHelper.QQMusicApi.GetLyricsAsync(qqResult.Id);
                token.ThrowIfCancellationRequested();

                lyricsSearchResult.Raw = response?.Lyrics;
                lyricsSearchResult.Translation = response?.Trans;
                lyricsSearchResult.Reference = $"https://y.qq.com/n/ryqq/songDetail/{qqResult.Mid}";
            }
            else if (result is NeteaseSearchResult neteaseResult)
            {
                var response = await ProviderHelper.NeteaseApi.GetLyric(neteaseResult.Id);
                token.ThrowIfCancellationRequested();

                lyricsSearchResult.Raw = response?.Lrc?.Lyric;
                lyricsSearchResult.Translation = response?.Tlyric?.Lyric;
                lyricsSearchResult.Transliteration = response?.Romalrc?.Lyric;
                lyricsSearchResult.Reference = $"https://music.163.com/song?id={neteaseResult.Id}";
            }
            else if (result is KugouSearchResult kugouResult)
            {
                var response = await ProviderHelper.KugouApi.GetSearchLyrics(hash: kugouResult.Hash);
                token.ThrowIfCancellationRequested();

                string? original = null;
                string? translated = null;
                var candidate = response?.Candidates.FirstOrDefault();
                if (candidate != null)
                {
                    original = await Helper.GetLyricsAsync(candidate.Id, candidate.AccessKey);
                    token.ThrowIfCancellationRequested();

                    if (original != null)
                    {
                        var parsedList = KrcParser.ParseLyrics(original);
                        if (parsedList != null)
                        {
                            translated = "";
                            foreach (var item in parsedList)
                                if (item is FullSyllableLineInfo fullSyllableLineInfo)
                                {
                                    var startTimeSpan = TimeSpan.FromMilliseconds(fullSyllableLineInfo.StartTime ?? 0);
                                    var startTimeStr = startTimeSpan.ToString(@"mm\:ss\.ff");
                                    var chTranslation = fullSyllableLineInfo.Translations.GetValueOrDefault("zh") ?? "";
                                    translated += $"[{startTimeStr}]{chTranslation}\n";
                                }
                        }
                    }

                    lyricsSearchResult.Reference = "https://www.kugou.com/";
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

    private async Task<LyricsCacheItem> SearchAppleMusicAsync(SongInfo songInfo, CancellationToken token)
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

        if (await _appleMusic.InitAsync(token))
            lyricsSearchResult = await _appleMusic.SearchSongInfoAsync(songInfo, token);

        return lyricsSearchResult;
    }

    private async Task<LyricsCacheItem> SearchPluginAsync(SongInfo songInfo, PluginInfo pluginInfo,
        CancellationToken token)
    {
        var plugin = (ILyricsSource)pluginInfo.Plugin!;
        var cacheItem = new LyricsCacheItem
        {
            Provider = (LyricsSearchProvider)_pluginService.GetPluginHashedId(pluginInfo.Id)
        };

        var result =
            await plugin.GetLyricsAsync(songInfo.Title, songInfo.Artist, songInfo.Album, songInfo.Duration, token);

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

        return cacheItem;
    }

    private async Task<LyricsCacheItem> SearchPluginAsync(SongInfo songInfo, LyricsSearchProvider provider,
        CancellationToken token)
    {
        var pluginInfo =
            _settingsService.AppSettings.PluginsInfo.FirstOrDefault(p =>
                _pluginService.GetPluginHashedId(p.Id) == (int)provider);
        if (pluginInfo == null) throw new ArgumentNullException(nameof(pluginInfo));

        return await SearchPluginAsync(songInfo, pluginInfo, token);
    }
}