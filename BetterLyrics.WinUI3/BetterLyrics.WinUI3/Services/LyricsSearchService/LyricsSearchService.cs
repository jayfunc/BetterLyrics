// 2025/6/23 by Zhe Fang

using ATL;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Helper.BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Providers;
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
using System.Text.Json.Nodes;
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
        private readonly ILogger _logger;

        public LyricsSearchService(ISettingsService settingsService, ILogger<LyricsSearchService> logger)
        {
            _settingsService = settingsService;
            _logger = logger;

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

        public async Task<LyricsSearchResult> SearchSmartlyAsync(SongInfo songInfo, CancellationToken token)
        {
            var lyricsSearchResult = new LyricsSearchResult();

            string overridenTitle = songInfo.Title;
            string[] overridenArtists = songInfo.Artists;
            string overridenAlbum = songInfo.Album;

            _logger.LogInformation("SearchSmartlyAsync {SongInfo}", songInfo);

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
                        targetProvider.Value, token);
                }
            }

            foreach (var provider in _settingsService.AppSettings.MediaSourceProvidersInfo.FirstOrDefault(x => x.Provider == songInfo.PlayerId)?.LyricsSearchProvidersInfo ?? [])
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
                    provider.Provider, token);

                if (lyricsSearchResult.IsFound)
                {
                    return lyricsSearchResult;
                }
            }

            return lyricsSearchResult;
        }

        public async Task<List<LyricsSearchResult>> SearchAllAsync(SongInfo songInfo, CancellationToken token)
        {
            _logger.LogInformation("SearchAllAsync {SongInfo}", songInfo);
            var results = new List<LyricsSearchResult>();
            foreach (var provider in Enum.GetValues<LyricsSearchProvider>())
            {
                var searchResult = await SearchSingleAsync(songInfo, provider, token);
                results.Add(searchResult);
            }
            return results;
        }

        private async Task<LyricsSearchResult> SearchSingleAsync(SongInfo songInfo, LyricsSearchProvider provider, CancellationToken token)
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
                    var cachedLyrics = FileHelper.ReadLyricsCache(songInfo, lyricsFormat, provider.GetCacheDirectory());
                    if (!string.IsNullOrWhiteSpace(cachedLyrics))
                    {
                        lyricsSearchResult.Raw = cachedLyrics;
                        lyricsSearchResult.CopyFromSongInfo(songInfo);
                        return lyricsSearchResult;
                    }
                }

                if (provider.IsLocal())
                {
                    if (provider == LyricsSearchProvider.LocalMusicFile)
                    {
                        lyricsSearchResult = SearchEmbedded(songInfo);
                    }
                    else
                    {
                        lyricsSearchResult = await SearchFile(songInfo, lyricsFormat);
                    }
                }
                else
                {
                    switch (provider)
                    {
                        case LyricsSearchProvider.LrcLib:
                            lyricsSearchResult = await SearchLrcLibAsync(songInfo);
                            break;
                        case LyricsSearchProvider.QQ:
                            lyricsSearchResult = await SearchQQNeteaseKugouAsync(songInfo, Searchers.QQMusic);
                            break;
                        case LyricsSearchProvider.Kugou:
                            lyricsSearchResult = await SearchQQNeteaseKugouAsync(songInfo, Searchers.Kugou);
                            break;
                        case LyricsSearchProvider.Netease:
                            lyricsSearchResult = await SearchQQNeteaseKugouAsync(songInfo, Searchers.Netease);
                            break;
                        case LyricsSearchProvider.AmllTtmlDb:
                            lyricsSearchResult = await SearchAmllTtmlDbAsync(songInfo);
                            break;
                        case LyricsSearchProvider.AppleMusic:
                            lyricsSearchResult = await SearchAppleMusicAsync(songInfo);
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
                        FileHelper.WriteLyricsCache(songInfo, lyricsSearchResult.Raw!, lyricsFormat, provider.GetCacheDirectory());
                    }
                }
            }
            catch (Exception)
            {
            }

            return lyricsSearchResult;
        }

        private async Task<LyricsSearchResult> SearchFile(SongInfo songInfo, LyricsFormat format)
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
                            var fileName = Path.GetFileNameWithoutExtension(file);
                            if (FileHelper.IsSwitchableNormalizedMatch(fileName, songInfo.Title, songInfo.DisplayArtists) || songInfo.LinkedFileName == fileName)
                            {
                                string? raw = await File.ReadAllTextAsync(file, FileHelper.GetEncoding(file));
                                if (raw != null)
                                {
                                    lyricsSearchResult.Raw = raw;
                                    lyricsSearchResult.CopyFromSongInfo(songInfo);

                                    return lyricsSearchResult;
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

        private LyricsSearchResult SearchEmbedded(SongInfo songInfo)
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
                            if ((songInfo.Album != "" && track.Title == songInfo.Title && track.Artist == songInfo.DisplayArtists && track.Album == songInfo.Album)
                                || (songInfo.Album == "" && track.Title == songInfo.Title && track.Artist == songInfo.DisplayArtists)
                                || (songInfo.Album == "" && FileHelper.IsSwitchableNormalizedMatch(Path.GetFileNameWithoutExtension(file), songInfo.Title, songInfo.DisplayArtists)))
                            {
                                var plain = track.GetRawLyrics();
                                if (!plain.IsNullOrEmpty())
                                {
                                    lyricsSearchResult.Raw = plain;
                                    lyricsSearchResult.CopyFromSongInfo(songInfo);

                                    return lyricsSearchResult;
                                }
                            }
                        }
                    }
                }
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
                            artists = valueArr.EnumerateArray().Select(x=>x.GetString()).Join(ATL.Settings.DisplayValueSeparator.ToString());
                    }
                    if (musicName == null || artists == null)
                        continue;

                    if (FileHelper.IsSwitchableNormalizedMatch($"{artists} - {musicName}", songInfo.Title, songInfo.DisplayArtists))
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
            var url = $"{_settingsService.AppSettings.GeneralSettings.AmllTtmlDbBaseUrl}/{Constants.AmllTTmlDB.QueryPrefix}/{rawLyricFile}";
            try
            {
                using var response = await _amllTtmlDbHttpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return lyricsSearchResult;
                }
                string lyrics = await response.Content.ReadAsStringAsync();

                lyricsSearchResult.Raw = lyrics;
                lyricsSearchResult.Title = songInfo.Title;
                lyricsSearchResult.Artists = songInfo.Artists;
                lyricsSearchResult.Album = songInfo.Album;
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

            if (jArr.ValueKind == JsonValueKind.Array && jArr.GetArrayLength() > 0)
            {
                var first = jArr[0];
                original = first.GetProperty("syncedLyrics").GetString();
                searchedTitle = first.GetProperty("trackName").GetString();
                searchedArtist = first.GetProperty("artistName").GetString();
                searchedAlbum = first.GetProperty("albumName").GetString();
            }

            lyricsSearchResult.Raw = original;
            lyricsSearchResult.Title = searchedTitle;
            lyricsSearchResult.Artists = searchedArtist?.Split(ATL.Settings.DisplayValueSeparator);
            lyricsSearchResult.Album = searchedAlbum;

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
            if (searcher == Searchers.Netease && songInfo.SongId != null)
            {
                result = new NeteaseSearchResult(songInfo.Title, songInfo.Artists, songInfo.Album, songInfo.Artists, (int)songInfo.DurationMs, songInfo.SongId);
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
                }, searcher, Lyricify.Lyrics.Searchers.Helpers.CompareHelper.MatchType.Medium);
            }

            if (result != null)
            {
                if (result is QQMusicSearchResult qqResult)
                {
                    var response = await Lyricify.Lyrics.Helpers.ProviderHelper.QQMusicApi.GetLyricsAsync(qqResult.Id);
                    var original = response?.Lyrics;
                    var translated = response?.Trans;
                    if (!string.IsNullOrEmpty(translated))
                    {
                        FileHelper.WriteLyricsCache(
                            songInfo,
                            translated,
                            LyricsFormat.Lrc,
                            PathHelper.QQTranslationCacheDirectory
                        );
                    }

                    lyricsSearchResult.Raw = original;
                }
                else if (result is NeteaseSearchResult neteaseResult)
                {
                    var response = await Lyricify.Lyrics.Helpers.ProviderHelper.NeteaseApi.GetLyric(neteaseResult.Id);
                    var original = response?.Lrc?.Lyric;
                    var translated = response?.Tlyric?.Lyric;
                    if (!string.IsNullOrEmpty(translated))
                    {
                        FileHelper.WriteLyricsCache(
                            songInfo,
                            translated,
                            LyricsFormat.Lrc,
                            PathHelper.NeteaseTranslationCacheDirectory
                        );
                    }

                    lyricsSearchResult.Raw = original;
                }
                else if (result is KugouSearchResult kugouResult)
                {
                    var response = await Lyricify.Lyrics.Helpers.ProviderHelper.KugouApi.GetSearchLyrics(hash: kugouResult.Hash);
                    string? original = null;
                    var candidate = response?.Candidates.FirstOrDefault();
                    if (candidate != null)
                    {
                        original = await Lyricify.Lyrics.Decrypter.Krc.Helper.GetLyricsAsync(candidate.Id, candidate.AccessKey);
                        if (original != null)
                        {
                            var parsedList = Lyricify.Lyrics.Parsers.KrcParser.ParseLyrics(original);
                            if (parsedList != null)
                            {
                                string translated = "";
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
                                if (!string.IsNullOrEmpty(translated))
                                {
                                    FileHelper.WriteLyricsCache(
                                        songInfo,
                                        translated,
                                        LyricsFormat.Lrc,
                                        PathHelper.KugouTranslationCacheDirectory
                                    );
                                }
                            }
                        }
                    }

                    lyricsSearchResult.Raw = original;
                }
            }

            lyricsSearchResult.Title = result?.Title;
            lyricsSearchResult.Artists = result?.Artists;
            lyricsSearchResult.Album = result?.Album;

            return lyricsSearchResult;
        }

        private async Task<LyricsSearchResult> SearchAppleMusicAsync(SongInfo songInfo)
        {
            var lyricsSearchResult = new LyricsSearchResult
            {
                Provider = LyricsSearchProvider.AppleMusic,
            };

            if (await _appleMusic.InitAsync())
            {
                var raw = await _appleMusic.GetLyricsAsync(songInfo.Title, songInfo.DisplayArtists);
                _logger.LogInformation("SearchAppleMusicAsync");
                lyricsSearchResult.Raw = raw;
                lyricsSearchResult.Title = songInfo.Title;
                lyricsSearchResult.Artists = songInfo.Artists;
                lyricsSearchResult.Album = "";
            }

            return lyricsSearchResult;
        }
    }
}
