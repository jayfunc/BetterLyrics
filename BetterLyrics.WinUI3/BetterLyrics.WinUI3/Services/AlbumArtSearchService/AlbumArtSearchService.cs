using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.FileSystemService;
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
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace BetterLyrics.WinUI3.Services.AlbumArtSearchService
{
    public class AlbumArtSearchService : IAlbumArtSearchService
    {
        private readonly HttpClient _iTunesHttpClinet;

        private readonly ISettingsService _settingsService;
        private readonly IFileSystemService _fileSystemService;
        private readonly ILogger _logger;

        public AlbumArtSearchService(ISettingsService settingsService, IFileSystemService fileSystemService, ILogger<AlbumArtSearchService> logger)
        {
            _settingsService = settingsService;
            _fileSystemService = fileSystemService;
            _logger = logger;
            _iTunesHttpClinet = new();
        }

        public async Task<IBuffer?> SearchAsync(SongInfo songInfo, IBuffer? bufferFromSMTC, CancellationToken token)
        {
            IBuffer? result = null;

            try
            {
                foreach (var provider in _settingsService.AppSettings.MediaSourceProvidersInfo.FirstOrDefault(x => x.Provider == songInfo.PlayerId)?.AlbumArtSearchProvidersInfo ?? [])
                {
                    if (!provider.IsEnabled)
                    {
                        continue;
                    }

                    switch (provider.Provider)
                    {
                        case AlbumArtSearchProvider.Local:
                            result = (await SearchFile(songInfo))?.AsBuffer();
                            break;
                        case AlbumArtSearchProvider.SMTC:
                            result = bufferFromSMTC;
                            break;
                        case AlbumArtSearchProvider.iTunes:
                            foreach (string countryCode in new List<string>() { "us", "cn", "jp", "kr" })
                            {
                                var byteArray = await SearchiTunesAsync(songInfo, countryCode);
                                result = byteArray?.AsBuffer();
                                if (token.IsCancellationRequested) return result;
                                if (result != null) break;
                            }
                            break;
                        default:
                            break;
                    }

                    if (result != null) return result;
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        private async Task<byte[]?> SearchFile(SongInfo songInfo)
        {
            var enabledIds = _settingsService.AppSettings.LocalMediaFolders
                .Where(f => f.IsEnabled)
                .Select(f => f.Id)
                .ToList();

            if (enabledIds.Count == 0) return null;

            var allFiles = await _fileSystemService.GetParsedFilesAsync(enabledIds);
            allFiles = allFiles.Where(x => FileHelper.MusicExtensions.Contains(Path.GetExtension(x.FileName))).ToList();

            FileCacheEntity? bestMatch = null;

            foreach (var item in allFiles)
            {
                var ext = Path.GetExtension(item.FileName).ToLower();
                if (!FileHelper.MusicExtensions.Contains(ext)) continue;

                bool isMetadataMatch = (item.Title == songInfo.Title && item.Artists == songInfo.DisplayArtists);

                bool isFilenameMatch = StringHelper.IsSwitchableNormalizedMatch(
                    Path.GetFileNameWithoutExtension(item.FileName),
                    songInfo.DisplayArtists,
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

        private async Task<byte[]?> SearchiTunesAsync(SongInfo songInfo, string countryCode)
        {
            // Source: https://gist.github.com/mcworkaholic/82fbf203e3f1043bbe534b5b2974c0ce
            try
            {
                string format = ".jpg";
                var cachedAlbumArt = FileHelper.ReadAlbumArtCache(songInfo.DisplayArtists, songInfo.Album, format, PathHelper.iTunesAlbumArtCacheDirectory);

                if (cachedAlbumArt != null)
                {
                    return cachedAlbumArt;
                }

                // Build the iTunes API URL
                string url = $"{Constants.iTunes.QueryPrefix}term=" + WebUtility.UrlEncode($"{songInfo.Artists} {songInfo.Album}").Replace("%20", "+") + "&country=" + countryCode + "&entity=album&media=music&limit=1";

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
                        string artworkUrl = artworkUrlProp.GetString()?.Replace("100x100bb.jpg", "1200x1200bb.jpg") ?? string.Empty;
                        var fetched = await _iTunesHttpClinet.GetByteArrayAsync(artworkUrl);

                        if (fetched != null && fetched.Length > 0)
                        {
                            // Write to cache
                            FileHelper.WriteAlbumArtCache(songInfo, fetched, format, PathHelper.iTunesAlbumArtCacheDirectory);
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

    }
}
