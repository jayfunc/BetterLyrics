using ATL;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Helper.BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
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
        private readonly ILogger _logger;

        public AlbumArtSearchService(ISettingsService settingsService, ILogger<AlbumArtSearchService> logger)
        {
            _settingsService = settingsService;
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
            foreach (var folder in _settingsService.AppSettings.LocalMediaFolders)
            {
                if (!folder.IsEnabled) continue;

                try
                {
                    using var fs = folder.CreateFileSystem();
                    if (fs == null) continue;
                    if (!await fs.ConnectAsync()) continue;

                    // 递归扫描
                    var foldersToScan = new Queue<string>();
                    foldersToScan.Enqueue(""); // 根目录

                    while (foldersToScan.Count > 0)
                    {
                        var currentPath = foldersToScan.Dequeue();
                        var items = await fs.GetFilesAsync(currentPath);

                        foreach (var item in items)
                        {
                            if (item.IsFolder)
                            {
                                foldersToScan.Enqueue(Path.Combine(currentPath, item.Name));
                                continue;
                            }

                            var ext = Path.GetExtension(item.Name).ToLower();
                            if (FileHelper.MusicExtensions.Contains(ext))
                            {
                                try
                                {
                                    using (var stream = await fs.OpenReadAsync(item.FullPath))
                                    {
                                        var track = new ExtendedTrack(item.FullPath, stream);

                                        bool isMetadataMatch = (track.Title == songInfo.Title && track.Artist == songInfo.DisplayArtists);
                                        bool isFilenameMatch = StringHelper.IsSwitchableNormalizedMatch(
                                            Path.GetFileNameWithoutExtension(item.Name),
                                            songInfo.DisplayArtists,
                                            songInfo.Title
                                        );

                                        if (isMetadataMatch || isFilenameMatch)
                                        {
                                            var bytes = track.EmbeddedPictures.FirstOrDefault()?.PictureData;
                                            if (bytes != null && bytes.Length > 0)
                                            {
                                                return bytes;
                                            }
                                        }
                                    }
                                }
                                catch
                                {
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
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
