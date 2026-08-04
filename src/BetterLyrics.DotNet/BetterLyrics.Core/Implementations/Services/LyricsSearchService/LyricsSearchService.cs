// 2025/6/23 by Zhe Fang

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
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

namespace BetterLyrics.Core.Implementations.Services.LyricsSearchService;

public class LyricsSearchService : ILyricsSearchService
{
    private readonly HttpClient _amllTtmlDbHttpClient;
    private readonly HttpClient _lrcLibHttpClient;
    private readonly Providers.AppleMusic _appleMusic;

    private readonly IFileSystemService _fileSystemService;
    private readonly ILogger _logger;
    private readonly ILyricsCacheService _lyricsCacheService;
    private readonly IPluginService _pluginService;
    private readonly IPasswordVaultProvider _passwordVaultProvider;
    private readonly ISettingsService _settingsService;
    private readonly ISongSearchMapService _songSearchMapService;

    public LyricsSearchService(
        ISettingsService settingsService,
        IFileSystemService fileSystemService,
        ILyricsCacheService lyricsCacheService,
        IPasswordVaultProvider passwordVaultProvider,
        ISongSearchMapService songSearchMapService,
        IPluginService pluginService,
        ILogger<LyricsSearchService> logger
    )
    {
        _settingsService = settingsService;
        _fileSystemService = fileSystemService;
        _lyricsCacheService = lyricsCacheService;
        _songSearchMapService = songSearchMapService;
        _passwordVaultProvider = passwordVaultProvider;
        _pluginService = pluginService;
        _logger = logger;

        _lrcLibHttpClient = new();
        _lrcLibHttpClient.DefaultRequestHeaders.Add(
            "User-Agent",
            $"{App.AppName} {MetadataHelper.AppVersion} ({Link.BetterLyricsGitHub})"
        );
        _amllTtmlDbHttpClient = new();
        _appleMusic = new();
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

            _logger.LogInformation("SearchSmartlyAsync: {SongInfo}", songInfo);

            try
            {
                var found = await _songSearchMapService.TryGetMappingAsync(songInfo, token);

                if (found != null)
                {
                    overridenTitle = found.MappedTitle;
                    overridenArtist = found.MappedArtist;
                    overridenAlbum = found.MappedAlbum;

                    _logger.LogInformation("SearchSmartlyAsync: Found mapped song search query: {MappedSongSearchQuery}", found);

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
                Debug.WriteLine(ex.Message);
                _logger.LogError(ex, "SearchSmartlyAsync: ");
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
                        _logger.LogWarning(ex, "SearchSmartlyAsync: Provider {Provider} failed during parallel search.", provider.Provider);
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
                        _logger.LogError(ex, "SearchSmartlyAsync: Provider {Provider} failed during sequential search.",
                            provider.Provider);
                    }
            }

            if (finalResult == null) throw new Exception("SearchSmartlyAsync: Could't find any lyrics");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SearchSmartlyAsync: An unexpected error occurred.");
            throw;
        }

        return finalResult;
    }

    public async IAsyncEnumerable<LyricsCacheItem> SearchAllAsync(
        SongInfo songInfo,
        bool checkCache,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SearchAllAsync: {SongInfo}", songInfo);

        var searchTasks = new List<Task<LyricsCacheItem>>();

        foreach (var provider in Enum.GetValues<LyricsProvider>().Where(p => !p.IsInternal()))
            searchTasks.Add(SearchSingleAsync(songInfo, provider, checkCache, cancellationToken));

        foreach (var plugin in _settingsService.AppSettings.PluginsInfo)
            if (plugin.Plugin is ILyricsSource)
            {
                var provider = (LyricsProvider)_pluginService.GetPluginHashedId(plugin.Plugin.Id);
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
                _logger.LogError(ex, "SearchAllAsync: A lyrics search provider failed or timed out.");
            }

            if (result != null) yield return result;
        }
    }

    public List<LyricsProvider> GetActiveProviders()
    {
        List<LyricsProvider> providers = [];

        foreach (var provider in Enum.GetValues<LyricsProvider>().Where(p => !p.IsInternal())) providers.Add(provider);

        foreach (var plugin in _settingsService.AppSettings.PluginsInfo)
            if (plugin.Plugin is ILyricsSource)
            {
                var provider = (LyricsProvider)_pluginService.GetPluginHashedId(plugin.Plugin.Id);
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

    private async Task<LyricsCacheItem> SearchSingleAsync(SongInfo songInfo, LyricsProvider provider,
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
                case LyricsProvider.QQ:
                    lyricsSearchResult = await SearchQQAsync(songInfo, token);
                    break;
                case LyricsProvider.Kugou:
                    lyricsSearchResult = await SearchKugouAsync(songInfo, token);
                    break;
                case LyricsProvider.Netease:
                    lyricsSearchResult = await SearchNeteaseAsync(songInfo, token);
                    break;
                case LyricsProvider.LrcLib:
                    lyricsSearchResult = await SearchLrcLibAsync(songInfo, token);
                    break;
                case LyricsProvider.AmllTtmlDb:
                    lyricsSearchResult = await SearchAmllTtmlDbAsync(songInfo, token);
                    break;
                case LyricsProvider.LocalMusicFile:
                    lyricsSearchResult = await SearchMusicFileAsync(songInfo, token);
                    break;
                case LyricsProvider.LocalLrcFile:
                case LyricsProvider.LocalEslrcFile:
                case LyricsProvider.LocalTtmlFile:
                    lyricsSearchResult = await SearchLyricsFileAsync(songInfo, provider.GetLyricsFormat(), token);
                    break;
                case LyricsProvider.AppleMusic:
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
        if (format.ToLyricsProvider() is LyricsProvider lyricsSearchProvider)
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
            lyricsSearchResult.Artist = bestFileEntity.Artists;
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
            Provider = LyricsProvider.LocalMusicFile
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
            lyricsSearchResult.Artist = bestFile.Artists;
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
            Provider = LyricsProvider.AmllTtmlDb
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

            string[] titles = [];
            string[] artists = [];
            string[] albums = [];
            string? ncmMusicId = null;

            foreach (var meta in metadataArr.EnumerateArray())
            {
                if (meta.GetArrayLength() != 2) continue;
                var key = meta[0].GetString();
                var valueArr = meta[1];
                if (key == "musicName" && valueArr.GetArrayLength() > 0) titles = valueArr.EnumerateArray().Select(x => x.GetString() ?? "").ToArray();
                if (key == "artists" && valueArr.GetArrayLength() > 0) artists = valueArr.EnumerateArray().Select(x => x.GetString() ?? "").ToArray();
                if (key == "album" && valueArr.GetArrayLength() > 0) albums = valueArr.EnumerateArray().Select(x => x.GetString() ?? "").ToArray();
                if (key == "ncmMusicId" && valueArr.GetArrayLength() > 0) ncmMusicId = valueArr[0].GetString();
            }

            var matchedById = ncmMusicId == songInfo.SongId && PlayerIdHelper.IsNeteaseFamily(songInfo.PlayerId);

            if (titles.Length == 0) titles = [""];
            if (artists.Length == 0) artists = [""];
            if (albums.Length == 0) albums = [""];

            int score = MetadataComparer.CalculateScore(songInfo, titles, artists, albums, lyricsSearchResult.Duration);

            if (matchedById || score > lyricsSearchResult.MatchPercentage)
            {
                if (root.TryGetProperty("rawLyricFile", out var rawLyricFileProp))
                {
                    bestNcmMusicId = ncmMusicId;
                    rawLyricFile = rawLyricFileProp.GetString();
                    lyricsSearchResult.Title = titles.FirstOrDefault();
                    lyricsSearchResult.Artist = string.Join("/", artists.Where(x => !string.IsNullOrEmpty(x)));
                    lyricsSearchResult.Album = albums.FirstOrDefault();
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
            var tmp = await SearchNeteaseAsync(
                ((SongInfo)songInfo.Clone()).WithSongId(
                    $"{ExtendedGenreFiled.NetEaseCloudMusicTrackID}{bestNcmMusicId}"), token);
            lyricsSearchResult.Duration = tmp.Duration;
            lyricsSearchResult.MatchPercentage = MetadataComparer.CalculateScore(songInfo, lyricsSearchResult);
        }

        return lyricsSearchResult;
    }

    private async Task<LyricsCacheItem> SearchLrcLibAsync(SongInfo songInfo, CancellationToken token)
    {
        var lyricsSearchResult = new LyricsCacheItem
        {
            Provider = LyricsProvider.LrcLib
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

    private static async Task<LyricsCacheItem> SearchQQAsync(SongInfo songInfo, CancellationToken token)
    {
        var lyricsSearchResult = new LyricsCacheItem
        {
            Provider = LyricsProvider.QQ
        };

        ISearchResult? result;

        if (songInfo.SongId != null && PlayerIdHelper.IsQQFamily(songInfo.PlayerId))
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
            }, Searchers.QQMusic, CompareHelper.MatchType.NoMatch);
            token.ThrowIfCancellationRequested();
        }

        if (result is QQMusicSearchResult qqResult)
        {
            var response = await ProviderHelper.QQMusicApi.GetLyricsAsync(qqResult.Id);
            token.ThrowIfCancellationRequested();

            lyricsSearchResult.Raw = response?.Lyrics;
            lyricsSearchResult.Translation = response?.Trans;
            lyricsSearchResult.Reference = $"https://y.qq.com/n/ryqq/songDetail/{qqResult.Mid}";
        }

        lyricsSearchResult.Title = result?.Title;
        lyricsSearchResult.Artist = result?.Artist;
        lyricsSearchResult.Album = result?.Album;
        lyricsSearchResult.Duration = result?.DurationMs / 1000;

        lyricsSearchResult.MatchPercentage = MetadataComparer.CalculateScore(songInfo, lyricsSearchResult);

        return lyricsSearchResult;
    }

    private static async Task<LyricsCacheItem> SearchNeteaseAsync(SongInfo songInfo, CancellationToken token)
    {
        var lyricsSearchResult = new LyricsCacheItem
        {
            Provider = LyricsProvider.Netease
        };

        ISearchResult? result;

        if (songInfo.SongId != null && PlayerIdHelper.IsNeteaseFamily(songInfo.PlayerId))
        {
            result = new NeteaseSearchResult(songInfo.Title, [songInfo.Artist], songInfo.Album, [],
                (int)songInfo.DurationMs, songInfo.SongId);
        }
        else
        {
            result = await SearchHelper.Search(new TrackMultiArtistMetadata
            {
                DurationMs = (int)songInfo.DurationMs,
                Album = songInfo.Album,
                Artist = songInfo.Artist,
                Title = songInfo.Title
            }, Searchers.Netease, CompareHelper.MatchType.NoMatch);
            token.ThrowIfCancellationRequested();
        }

        if (result is NeteaseSearchResult neteaseResult)
        {
            var response = await ProviderHelper.NeteaseApi.GetLyric(neteaseResult.Id);
            token.ThrowIfCancellationRequested();

            lyricsSearchResult.Raw = response?.Lrc?.Lyric;
            lyricsSearchResult.Translation = response?.Tlyric?.Lyric;
            lyricsSearchResult.Transliteration = response?.Romalrc?.Lyric;
            lyricsSearchResult.Reference = $"https://music.163.com/song?id={neteaseResult.Id}";
        }

        lyricsSearchResult.Title = result?.Title;
        lyricsSearchResult.Artist = result?.Artist;
        lyricsSearchResult.Album = result?.Album;
        lyricsSearchResult.Duration = result?.DurationMs / 1000;

        lyricsSearchResult.MatchPercentage = MetadataComparer.CalculateScore(songInfo, lyricsSearchResult);

        return lyricsSearchResult;
    }

    private static async Task<LyricsCacheItem> SearchKugouAsync(SongInfo songInfo, CancellationToken token)
    {
        var lyricsSearchResult = new LyricsCacheItem
        {
            Provider = LyricsProvider.Kugou
        };

        ISearchResult? result;

        result = await SearchHelper.Search(new TrackMultiArtistMetadata
        {
            DurationMs = (int)songInfo.DurationMs,
            Album = songInfo.Album,
            Artist = songInfo.Artist,
            Title = songInfo.Title
        }, Searchers.Kugou, CompareHelper.MatchType.NoMatch);
        token.ThrowIfCancellationRequested();

        if (result is KugouSearchResult kugouResult)
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

                lyricsSearchResult.Reference = $"https://www.kugou.com/song/#hash={kugouResult.Hash}";
            }

            lyricsSearchResult.Raw = original;
            lyricsSearchResult.Translation = translated;
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
        await _appleMusic.InitAsync(token);
        return await _appleMusic.SearchSongInfoAsync(songInfo, token);
    }

    private async Task<LyricsCacheItem> SearchPluginAsync(SongInfo songInfo, PluginInfo pluginInfo,
        CancellationToken token)
    {
        var plugin = (ILyricsSource)pluginInfo.Plugin!;
        var cacheItem = new LyricsCacheItem
        {
            Provider = (LyricsProvider)_pluginService.GetPluginHashedId(pluginInfo.Id)
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

    private async Task<LyricsCacheItem> SearchPluginAsync(SongInfo songInfo, LyricsProvider provider,
        CancellationToken token)
    {
        var pluginInfo =
            _settingsService.AppSettings.PluginsInfo.FirstOrDefault(p =>
                _pluginService.GetPluginHashedId(p.Id) == (int)provider);
        if (pluginInfo == null) throw new ArgumentNullException(nameof(pluginInfo));

        return await SearchPluginAsync(songInfo, pluginInfo, token);
    }
}