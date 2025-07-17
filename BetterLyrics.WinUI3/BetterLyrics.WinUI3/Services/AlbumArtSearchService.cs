using ATL;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services
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

        public async Task<byte[]?> SearchAsync(string title, string artist, string album, byte[]? bytesFromSMTC = null)
        {
            byte[]? result = null;

            foreach (var provider in _settingsService.AlbumArtSearchProvidersInfo)
            {
                if (!provider.IsEnabled)
                {
                    continue;
                }

                switch (provider.Provider)
                {
                    case AlbumArtSearchProvider.Local:
                        result = SearchFile(artist, album);
                        break;
                    case AlbumArtSearchProvider.SMTC:
                        result = bytesFromSMTC;
                        break;
                    case AlbumArtSearchProvider.iTunes:
                        foreach (string countryCode in new List<string>() { "us", "cn", "jp", "kr" })
                        {
                            result = await SearchiTunesAsync(artist, album, title, countryCode);
                            if (result != null) break;
                        }
                        break;
                    default:
                        break;
                }

                if (result != null) return result;
            }
            return null;
        }

        private byte[]? SearchFile(string artist, string album)
        {
            foreach (var folder in _settingsService.LocalLyricsFolders)
            {
                if (Directory.Exists(folder.Path) && folder.IsEnabled)
                {
                    foreach (var file in Directory.GetFiles(folder.Path, $"*.*", SearchOption.AllDirectories))
                    {
                        if (FileHelper.IsSwitchableNormalizedMatch(Path.GetFileNameWithoutExtension(file), album, artist))
                        {
                            Track track = new(file);
                            var bytes = track.EmbeddedPictures.FirstOrDefault()?.PictureData;
                            if (bytes != null)
                            {
                                return bytes;
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
                string url = $"https://itunes.apple.com/search?term=" + WebUtility.UrlEncode($"{artist} {album}").Replace("%20", "+") + "&country=" + countryCode + "&entity=album&media=music&limit=1";

                // Make a request to the API
                HttpResponseMessage response = await _iTunesHttpClinet.GetAsync(url);
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
