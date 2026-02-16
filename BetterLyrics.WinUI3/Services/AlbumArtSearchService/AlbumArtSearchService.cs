using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Entities;
using BetterLyrics.WinUI3.Services.FileSystemService;
using BetterLyrics.WinUI3.Services.GSMTCService;
using BetterLyrics.WinUI3.Services.SettingsService;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace BetterLyrics.WinUI3.Services.AlbumArtSearchService
{
    public class AlbumArtSearchService : IAlbumArtSearchService
    {
        private readonly HttpClient _iTunesHttpClinet = new();
        private readonly HttpClient _kugouHttpClient = new();

        private readonly ISettingsService _settingsService;
        private readonly IFileSystemService _fileSystemService;
        private readonly ILogger _logger;

        public AlbumArtSearchService(ISettingsService settingsService, IFileSystemService fileSystemService, ILogger<AlbumArtSearchService> logger)
        {
            _settingsService = settingsService;
            _fileSystemService = fileSystemService;
            _logger = logger;
        }

        public async Task<IBuffer?> SearchAsync(SongInfo songInfo, IBuffer? bufferFromSMTC, bool ignoreCache, CancellationToken token)
        {
            string format = ".jpg";
            byte[]? result = null;

            try
            {
                var mediaSourceProviderInfo = _settingsService.AppSettings.MediaSourceProvidersInfo.FirstOrDefault(x => x.Provider == songInfo.PlayerId);
                int size = mediaSourceProviderInfo?.TargetAlbumArtSize ?? 500;

                foreach (var provider in mediaSourceProviderInfo?.AlbumArtSearchProvidersInfo ?? [])
                {
                    if (token.IsCancellationRequested) break;

                    if (!provider.IsEnabled)
                    {
                        continue;
                    }

                    if (!ignoreCache && provider.Provider.IsRemote())
                    {
                        var cachedAlbumArt = FileHelper.ReadAlbumArtCache(songInfo.Artist, songInfo.Album, format, provider.Provider.GetCacheDirectory());

                        if (cachedAlbumArt != null)
                        {
                            return cachedAlbumArt.AsBuffer();
                        }
                    }

                    switch (provider.Provider)
                    {
                        case AlbumArtSearchProvider.Local:
                            result = await SearchFileAsync(songInfo);
                            break;
                        case AlbumArtSearchProvider.SMTC:
                            if (bufferFromSMTC != null) return bufferFromSMTC;
                            break;
                        case AlbumArtSearchProvider.iTunes:
                            foreach (string countryCode in new List<string>() { "us", "cn", "jp", "kr" })
                            {
                                result = await SearchiTunesAsync(songInfo, countryCode, size);
                                if (result != null) break;
                            }
                            break;
                        case AlbumArtSearchProvider.Kugou:
                            result = await SearchKugouAsync(songInfo, size);
                            break;
                        //case AlbumArtSearchProvider.Netease:
                        //    result = await SearchNeteaseAsync(songInfo, size);
                        //    break;
                        default:
                            break;
                    }

                    if (result != null)
                    {
                        if (provider.Provider.IsRemote())
                        {
                            FileHelper.WriteAlbumArtCache(songInfo, result, format, provider.Provider.GetCacheDirectory());
                        }
                        return result.AsBuffer();
                    }
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        private async Task<byte[]?> SearchFileAsync(SongInfo songInfo)
        {
            var enabledIds = _settingsService.AppSettings.LocalMediaFolders
                .Where(f => f.IsEnabled)
                .Select(f => f.Id)
                .ToList();

            if (enabledIds.Count == 0) return null;

            var allFiles = await _fileSystemService.GetParsedFilesAsync(enabledIds);
            allFiles = allFiles.Where(x => FileHelper.MusicExtensions.Contains(Path.GetExtension(x.FileName))).ToList();

            FilesIndexItem? bestMatch = null;

            foreach (var item in allFiles)
            {
                var ext = Path.GetExtension(item.FileName).ToLower();
                if (!FileHelper.MusicExtensions.Contains(ext)) continue;

                bool isMetadataMatch = (item.Title == songInfo.Title && item.Artist == songInfo.Artist);

                bool isFilenameMatch = StringHelper.IsSwitchableNormalizedMatch(
                    Path.GetFileNameWithoutExtension(item.FileName),
                    songInfo.Artist,
                    songInfo.Title
                );

                if (isMetadataMatch || isFilenameMatch)
                {
                    bestMatch = item;
                    break;
                }
            }

            if (bestMatch == null || string.IsNullOrEmpty(bestMatch.LocalAlbumArtPath))
            {
                return null;
            }

            try
            {
                if (File.Exists(bestMatch.LocalAlbumArtPath))
                {
                    return await File.ReadAllBytesAsync(bestMatch.LocalAlbumArtPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"读取本地缓存失败: {ex.Message}");
            }

            return null;
        }

        private async Task<byte[]?> SearchiTunesAsync(SongInfo songInfo, string countryCode, int size)
        {
            // Source: https://gist.github.com/mcworkaholic/82fbf203e3f1043bbe534b5b2974c0ce
            try
            {
                // Build the iTunes API URL
                string url = $"{Constants.iTunes.QueryPrefix}term=" + WebUtility.UrlEncode($"{songInfo.Artist} {songInfo.Album}").Replace("%20", "+") + "&country=" + countryCode + "&entity=album&media=music&limit=1";

                // Make a request to the API
                using HttpResponseMessage response = await _iTunesHttpClinet.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                // Parse the JSON response
                var data = JsonSerializer.Deserialize(responseBody, Serialization.SourceGenerationContext.Default.JsonElement);

                if (data.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array && results.GetArrayLength() > 0)
                {
                    // Get the first result
                    var result = results[0];
                    if (result.TryGetProperty("artworkUrl100", out var artworkUrlProp))
                    {
                        string artworkUrl = artworkUrlProp.GetString()?.Replace("100x100bb.jpg", $"{size}x{size}bb.jpg") ?? string.Empty;
                        var fetched = await _iTunesHttpClinet.GetByteArrayAsync(artworkUrl);

                        if (fetched != null && fetched.Length > 0)
                        {
                            return fetched;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SearchiTunesAsync");
            }
            return null;
        }

        private async Task<byte[]?> SearchKugouAsync(SongInfo songInfo, int size)
        {
            try
            {
                string keyword = $"{songInfo.Title} {songInfo.Artist}".Trim();
                if (string.IsNullOrEmpty(keyword)) return null;

                string searchUrl = $"http://mobilecdn.kugou.com/api/v3/search/song?format=json&keyword={Uri.EscapeDataString(keyword)}&page=1&pagesize=1&showtype=1";

                if (!_kugouHttpClient.DefaultRequestHeaders.Contains("User-Agent"))
                {
                    _kugouHttpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                }

                string searchResponse = await _kugouHttpClient.GetStringAsync(searchUrl);

                var searchJson = JsonNode.Parse(searchResponse);
                var songs = searchJson?["data"]?["info"]?.AsArray();

                if (songs == null || songs.Count == 0)
                {
                    return null;
                }

                string? hash = songs[0]?["hash"]?.ToString();
                string? albumId = songs[0]?["album_id"]?.ToString();

                if (string.IsNullOrEmpty(hash)) return null;

                string detailsUrl = $"http://m.kugou.com/app/i/getSongInfo.php?cmd=playInfo&hash={hash}";

                string detailsResponse = await _kugouHttpClient.GetStringAsync(detailsUrl);
                var detailsJson = JsonNode.Parse(detailsResponse);

                string? imgUrl = detailsJson?["album_img"]?.ToString() ?? detailsJson?["img"]?.ToString();

                if (string.IsNullOrEmpty(imgUrl)) return null;

                imgUrl = imgUrl.Replace("{size}", $"{size}");

                byte[] imageBytes = await _kugouHttpClient.GetByteArrayAsync(imgUrl);

                return imageBytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SearchKugouAsync");
                return null;
            }
        }
    }
}
