using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Serialization;
using Microsoft.Extensions.Logging;

namespace BetterLyrics.Core.Implementations.Services;

public class AlbumArtSearchService : IAlbumArtSearchService
{
    private readonly IFileSystemService _fileSystemService;
    private readonly HttpClient _iTunesHttpClinet = new();
    private readonly HttpClient _kugouHttpClient = new();
    private readonly ILogger _logger;

    private readonly ISettingsService _settingsService;

    public AlbumArtSearchService(ISettingsService settingsService, IFileSystemService fileSystemService,
        ILogger<AlbumArtSearchService> logger)
    {
        _settingsService = settingsService;
        _fileSystemService = fileSystemService;
        _logger = logger;
    }

    public async Task<byte[]?> SearchAsync(SongInfo songInfo, byte[]? bufferFromSMTC, bool ignoreCache,
        CancellationToken token)
    {
        var format = ".jpg";

        try
        {
            var mediaSourceProviderInfo =
                _settingsService.AppSettings.MediaSourceProvidersInfo.FirstOrDefault(x =>
                    x.Provider == songInfo.PlayerId);
            if (mediaSourceProviderInfo == null) return null;

            var providers = mediaSourceProviderInfo.AlbumArtSearchProvidersInfo;
            var size = mediaSourceProviderInfo.TargetAlbumArtSize;

            foreach (var providerInfo in providers)
            {
                if (!providerInfo.IsEnabled) continue;

                try
                {
                    token.ThrowIfCancellationRequested();

                    byte[]? result = null;

                    if (!ignoreCache && providerInfo.Provider.IsRemote())
                        try
                        {
                            var cachedAlbumArt = FileHelper.ReadAlbumArtCache(songInfo, format,
                                providerInfo.Provider.GetCacheDirectory());
                            if (cachedAlbumArt != null) return cachedAlbumArt;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to read album art cache.");
                        }

                    switch (providerInfo.Provider)
                    {
                        case AlbumArtSearchProvider.Local:
                            result = await SearchFileAsync(songInfo, token);
                            break;

                        case AlbumArtSearchProvider.SMTC:
                            if (bufferFromSMTC != null) return bufferFromSMTC;
                            break;

                        case AlbumArtSearchProvider.iTunes:
                            foreach (var countryCode in new List<string> { "us", "cn", "jp", "kr" })
                                try
                                {
                                    if (token.IsCancellationRequested) break;
                                    result = await SearchiTunesAsync(songInfo, countryCode, size, token);
                                    if (result != null) break;
                                }
                                catch (OperationCanceledException)
                                {
                                    throw;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogDebug(ex, "iTunes search failed for country {CountryCode}", countryCode);
                                }

                            break;

                        case AlbumArtSearchProvider.Kugou:
                            result = await SearchKugouAsync(songInfo, size, token);
                            break;

                        // case AlbumArtSearchProvider.Netease:
                        //     result = await SearchNeteaseAsync(songInfo, size);
                        //     break;
                    }

                    if (result != null)
                    {
                        if (providerInfo.Provider.IsRemote())
                            try
                            {
                                FileHelper.WriteAlbumArtCache(songInfo, result, format,
                                    providerInfo.Provider.GetCacheDirectory());
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "Failed to write album art cache.");
                            }

                        return result;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Provider {Provider} failed to search album art.", providerInfo.Provider);
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in SearchAsync.");
        }

        return null;
    }

    public async Task<string?> GetAlbumArtUrlAsync(SongInfo songInfo, DiscordAlbumArtSource source, int size, CancellationToken token)
    {
        try
        {
            switch (source)
            {
                case DiscordAlbumArtSource.iTunes:
                    foreach (var countryCode in new List<string> { "us", "cn", "jp", "kr" })
                    {
                        try
                        {
                            if (token.IsCancellationRequested) break;
                            var url = await GetiTunesUrlAsync(songInfo, countryCode, size, token);
                            if (url != null) return url;
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "iTunes URL search failed for country {CountryCode}", countryCode);
                        }
                    }
                    break;

                case DiscordAlbumArtSource.Kugou:
                    return await GetKugouUrlAsync(songInfo, size, token);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in GetAlbumArtUrlAsync.");
        }

        return null;
    }

    private async Task<byte[]?> SearchFileAsync(SongInfo songInfo, CancellationToken token)
    {
        var enabledIds = _settingsService.AppSettings.LocalMediaFolders
            .Where(f => f.IsEnabled)
            .Select(f => f.Id)
            .ToList();

        if (enabledIds.Count == 0) return null;

        var allFiles = await _fileSystemService.GetParsedFilesAsync(enabledIds, token);
        allFiles = allFiles.Where(x => FileHelper.MusicExtensions.Contains(Path.GetExtension(x.FileName).ToLower()))
            .ToList();

        var bestScore = 0;
        FilesIndexItem? bestMatch = null;

        foreach (var item in allFiles)
        {
            var ext = Path.GetExtension(item.FileName).ToLower();
            if (!FileHelper.MusicExtensions.Contains(ext)) continue;

            var score = MetadataComparer.CalculateScore(songInfo, item);
            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = item;
            }
        }

        if (bestMatch == null || string.IsNullOrEmpty(bestMatch.LocalAlbumArtPath)) return null;

        try
        {
            if (File.Exists(bestMatch.LocalAlbumArtPath))
                return await File.ReadAllBytesAsync(bestMatch.LocalAlbumArtPath, token);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"读取本地缓存失败: {ex.Message}");
            _logger.LogError(ex, "Failed to read local album art cache for {Artist} - {Album}", songInfo.Artist,
                songInfo.Album);
        }

        return null;
    }

    private async Task<string?> GetiTunesUrlAsync(SongInfo songInfo, string countryCode, int size, CancellationToken token)
    {
        // Source: https://gist.github.com/mcworkaholic/82fbf203e3f1043bbe534b5b2974c0ce

        var keyword = songInfo.ToSearchString();
        if (string.IsNullOrWhiteSpace(keyword)) return null;

        // Build the iTunes API URL
        var url = $"{iTunes.QueryPrefix}term=" + WebUtility.UrlEncode(keyword).Replace("%20", "+") + "&country=" +
                  countryCode + "&entity=album&media=music&limit=1";

        // Make a request to the API
        using var response = await _iTunesHttpClinet.GetAsync(url, token);
        response.EnsureSuccessStatusCode();
        var responseBody = await response.Content.ReadAsStringAsync(token);

        // Parse the JSON response
        var data = JsonSerializer.Deserialize(responseBody, SourceGenerationContext.Default.JsonElement);

        if (data.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array &&
            results.GetArrayLength() > 0)
        {
            // Get the first result
            var result = results[0];
            if (result.TryGetProperty("artworkUrl100", out var artworkUrlProp))
            {
                return artworkUrlProp.GetString()?.Replace("100x100bb.jpg", $"{size}x{size}bb.jpg");
            }
        }

        return null;
    }

    private async Task<byte[]?> SearchiTunesAsync(SongInfo songInfo, string countryCode, int size,
        CancellationToken token)
    {
        var artworkUrl = await GetiTunesUrlAsync(songInfo, countryCode, size, token);
        if (!string.IsNullOrEmpty(artworkUrl))
        {
            var fetched = await _iTunesHttpClinet.GetByteArrayAsync(artworkUrl, token);
            if (fetched != null && fetched.Length > 0) return fetched;
        }

        return null;
    }

    private async Task<string?> GetKugouUrlAsync(SongInfo songInfo, int size, CancellationToken token)
    {
        var keyword = songInfo.ToSearchString();
        if (string.IsNullOrWhiteSpace(keyword)) return null;

        var searchUrl =
            $"http://mobilecdn.kugou.com/api/v3/search/song?format=json&keyword={Uri.EscapeDataString(keyword)}&page=1&pagesize=1&showtype=1";

        if (!_kugouHttpClient.DefaultRequestHeaders.Contains("User-Agent"))
            _kugouHttpClient.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");

        var searchResponse = await _kugouHttpClient.GetStringAsync(searchUrl, token);

        var searchJson = JsonNode.Parse(searchResponse);
        var songs = searchJson?["data"]?["info"]?.AsArray();

        if (songs == null || songs.Count == 0) return null;

        var hash = songs[0]?["hash"]?.ToString();
        var albumId = songs[0]?["album_id"]?.ToString();

        if (string.IsNullOrEmpty(hash)) return null;

        var detailsUrl = $"http://m.kugou.com/app/i/getSongInfo.php?cmd=playInfo&hash={hash}";

        var detailsResponse = await _kugouHttpClient.GetStringAsync(detailsUrl, token);
        var detailsJson = JsonNode.Parse(detailsResponse);

        var imgUrl = detailsJson?["album_img"]?.ToString() ?? detailsJson?["img"]?.ToString();

        if (string.IsNullOrEmpty(imgUrl)) return null;

        return imgUrl.Replace("{size}", $"{size}");
    }

    private async Task<byte[]?> SearchKugouAsync(SongInfo songInfo, int size, CancellationToken token)
    {
        var imgUrl = await GetKugouUrlAsync(songInfo, size, token);
        if (!string.IsNullOrEmpty(imgUrl))
        {
            var imageBytes = await _kugouHttpClient.GetByteArrayAsync(imgUrl, token);
            return imageBytes;
        }

        return null;
    }
}