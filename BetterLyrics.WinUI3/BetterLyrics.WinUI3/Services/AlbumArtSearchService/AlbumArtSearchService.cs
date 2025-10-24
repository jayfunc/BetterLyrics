using ATL;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Helper.BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.DependencyInjection;
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

        public AlbumArtSearchService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            _logger = Ioc.Default.GetRequiredService<ILogger<AlbumArtSearchService>>();
            _iTunesHttpClinet = new();
        }

        public async Task<IBuffer?> SearchAsync(string mediaSessionId, string title, string artist, string album, IBuffer? bufferFromSMTC, CancellationToken token)
        {
            IBuffer? result = null;

            try
            {
                foreach (var provider in _settingsService.AppSettings.MediaSourceProvidersInfo.Where(x => x.Provider == mediaSessionId).FirstOrDefault()?.AlbumArtSearchProvidersInfo ?? [])
                {
                    if (!provider.IsEnabled)
                    {
                        continue;
                    }

                    switch (provider.Provider)
                    {
                        case AlbumArtSearchProvider.Local:
                            result = SearchFile(artist, title)?.AsBuffer();
                            break;
                        case AlbumArtSearchProvider.SMTC:
                            result = bufferFromSMTC;
                            break;
                        case AlbumArtSearchProvider.iTunes:
                            foreach (string countryCode in new List<string>() { "us", "cn", "jp", "kr" })
                            {
                                var byteArray = await SearchiTunesAsync(artist, album, title, countryCode);
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

        private byte[]? SearchFile(string artist, string title)
        {
            foreach (var folder in _settingsService.AppSettings.LocalMediaFolders)
            {
                if (Directory.Exists(folder.Path) && folder.IsEnabled)
                {
                    foreach (var file in DirectoryHelper.GetAllFiles(folder.Path))
                    {
                        if (FileHelper.MusicExtensions.Contains(Path.GetExtension(file)))
                        {
                            Track track = new(file);
                            if ((track.Title == title && track.Artist == artist) || FileHelper.IsSwitchableNormalizedMatch(Path.GetFileNameWithoutExtension(file), artist, title))
                            {
                                var bytes = track.EmbeddedPictures.FirstOrDefault()?.PictureData;
                                if (bytes != null)
                                {
                                    return bytes;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        private async Task<byte[]?> SearchiTunesAsync(string artist, string album, string title, string countryCode)
        {
            // Source: https://gist.github.com/mcworkaholic/82fbf203e3f1043bbe534b5b2974c0ce
            try
            {
                string format = ".jpg";
                var cachedAlbumArt = FileHelper.ReadAlbumArtCache(artist, album, format, PathHelper.iTunesAlbumArtCacheDirectory);

                if (cachedAlbumArt != null)
                {
                    return cachedAlbumArt;
                }

                // Build the iTunes API URL
                string url = $"{Constants.iTunes.QueryPrefix}term=" + WebUtility.UrlEncode($"{artist} {album}").Replace("%20", "+") + "&country=" + countryCode + "&entity=album&media=music&limit=1";

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
                            FileHelper.WriteAlbumArtCache(artist, album, fetched, format, PathHelper.iTunesAlbumArtCacheDirectory);
                            return fetched;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching iTunes album art for {Artist} - {Album}", artist, album);
            }
            return null;
        }

    }
}
