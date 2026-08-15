using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Serialization;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace BetterLyrics.Core.Implementations.Services.LyricsSearchService.Providers;

public class AppleMusic
{
    private readonly HttpClient _client;

    private readonly IPasswordVaultProvider _passwordVaultProvider =
        Ioc.Default.GetRequiredService<IPasswordVaultProvider>();

    private string _accessToken = "";
    private bool _isInited;
    private string _language = "";
    private string _storefront = "";

    public AppleMusic(IHttpClientFactory httpClientFactory)
    {
        _client = httpClientFactory.CreateClient();
        _client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36");
        _client.DefaultRequestHeaders.Add("Accept", "application/json");
        _client.DefaultRequestHeaders.Add("Origin", "https://music.apple.com");
        _client.DefaultRequestHeaders.Add("Referer", "https://music.apple.com/");
    }

    public async Task<bool> InitAsync(CancellationToken cancellationToken)
    {
        if (!_isInited)
        {
            var mediaUserToken = _passwordVaultProvider.Get(App.AppName,
                Constants.AppleMusic.MediaUserTokenKey);
            if (!string.IsNullOrEmpty(mediaUserToken))
            {
                if (!_client.DefaultRequestHeaders.Contains("media-user-token"))
                {
                    _client.DefaultRequestHeaders.Add("media-user-token", mediaUserToken);
                }

                await GetAccessTokenAsync(cancellationToken);
                await SetMediaUserTokenAsync(mediaUserToken, cancellationToken);
                _isInited = !string.IsNullOrEmpty(_accessToken);
            }
        }

        return _isInited;
    }

    private async Task GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        var resp = await _client.GetStringAsync("https://music.apple.com/us/browse", cancellationToken);
        var jsUrls = Regex.Matches(resp, "(?<url>(?:https://music\\.apple\\.com)?/?assets/index(?!-legacy)[^\\\"'<>\\s]*?\\.js)", RegexOptions.IgnoreCase)
            .Cast<Match>()
            .Select(x => NormalizeAppleMusicAssetUrl(x.Groups["url"].Value))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (jsUrls.Count == 0)
        {
            jsUrls = Regex.Matches(resp, "(?<url>(?:https://music\\.apple\\.com)?/?assets/index[^\\\"'<>\\s]*?\\.js)", RegexOptions.IgnoreCase)
                .Cast<Match>()
                .Select(x => NormalizeAppleMusicAssetUrl(x.Groups["url"].Value))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        if (jsUrls.Count == 0) throw new Exception("Failed to find index*.js");

        foreach (var jsUrl in jsUrls)
        {
            var jsResp = await _client.GetStringAsync(jsUrl, cancellationToken);
            
            var token = Regex.Matches(jsResp, @"eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+")
                .Cast<Match>()
                .Select(x => x.Value)
                .Distinct(StringComparer.Ordinal)
                .Select(t => new { Token = t, Score = GetAccessTokenScore(t) })
                .Where(x => x.Score >= 0)
                .OrderByDescending(x => x.Score)
                .Select(x => x.Token)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(token))
            {
                _accessToken = token;
                _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);
                return;
            }
        }

        throw new Exception("Failed to find access token");
    }

    private static string NormalizeAppleMusicAssetUrl(string url)
    {
        url = (url ?? string.Empty).Trim();
        if (url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return url;
        if (url.StartsWith("/", StringComparison.Ordinal)) return "https://music.apple.com" + url;
        return "https://music.apple.com/" + url;
    }

    private static int GetAccessTokenScore(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return -1;

            var headerJson = Encoding.UTF8.GetString(Convert.FromBase64String(NormalizeBase64(parts[0])));
            var payloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(NormalizeBase64(parts[1])));

            using var headerDoc = JsonDocument.Parse(headerJson);
            using var payloadDoc = JsonDocument.Parse(payloadJson);

            var exp = payloadDoc.RootElement.TryGetProperty("exp", out var expEl) ? expEl.GetInt64() : (long?)null;
            if (!exp.HasValue || DateTimeOffset.UtcNow >= DateTimeOffset.FromUnixTimeSeconds(exp.Value).AddMinutes(-1))
                return -1;

            var score = 0;
            var kid = headerDoc.RootElement.TryGetProperty("kid", out var kidEl) ? kidEl.GetString() : string.Empty;
            var issuer = payloadDoc.RootElement.TryGetProperty("iss", out var issEl) ? issEl.GetString() : string.Empty;

            if (string.Equals(kid, "WebPlayKid", StringComparison.OrdinalIgnoreCase)) score += 100;
            if (string.Equals(issuer, "AMPWebPlay", StringComparison.OrdinalIgnoreCase)) score += 100;
            if (payloadDoc.RootElement.TryGetProperty("root_https_origin", out _)) score += 10;

            return score;
        }
        catch
        {
            return -1;
        }
    }

    private static string NormalizeBase64(string value)
    {
        value = value.Replace('-', '+').Replace('_', '/');
        return value.PadRight(value.Length + (4 - value.Length % 4) % 4, '=');
    }

    private async Task SetMediaUserTokenAsync(string token, CancellationToken cancellationToken)
    {
        // the token is dynamically loaded from _passwordVaultProvider inside InitAsync
        var resp = await _client.GetStringAsync("https://amp-api.music.apple.com/v1/me/storefront",
            cancellationToken);
        var json = JsonSerializer.Deserialize(resp, SourceGenerationContext.Default.JsonElement);
        _storefront = json.GetProperty("data")[0].GetProperty("id").ToString();
        _language = json.GetProperty("data")[0].GetProperty("attributes").GetProperty("defaultLanguageTag")
            .ToString();

        if (!string.IsNullOrEmpty(_language) && !_client.DefaultRequestHeaders.Contains("Accept-Language"))
        {
            _client.DefaultRequestHeaders.Add("Accept-Language", $"{_language},en;q=0.9");
        }
    }

    private async Task<string?> GetLyricsAsync(string id, CancellationToken token)
    {
        var apiUrl = $"https://amp-api.music.apple.com/v1/catalog/{_storefront}/songs/{id}";
        var url = apiUrl + $"?include[songs]=lyrics,syllable-lyrics&l={WebUtility.UrlEncode(_language)}&extend=ttmlLocalizations";
        var resp = await _client.GetStringAsync(url, token);
        var json = JsonSerializer.Deserialize(resp, SourceGenerationContext.Default.JsonElement);
        var data = json.GetProperty("data");
        if (data.GetArrayLength() == 0) return string.Empty;
        var song = data[0];

        if (!song.TryGetProperty("relationships", out var relationships))
            return string.Empty;

        if (relationships.TryGetProperty("syllable-lyrics", out var syllableLyrics) &&
            syllableLyrics.GetProperty("data").GetArrayLength() > 0)
        {
            var syllableLyric = syllableLyrics.GetProperty("data")[0];
            if (syllableLyric.TryGetProperty("attributes", out var attributes) &&
                attributes.TryGetProperty("ttml", out var ttml))
            {
                var raw = ttml.GetString();
                if (raw != null && raw.Contains("begin=") && raw.Contains("end=")) return raw;
            }
        }

        if (relationships.TryGetProperty("lyrics", out var lyrics) &&
            lyrics.GetProperty("data").GetArrayLength() > 0)
        {
            var lyric = lyrics.GetProperty("data")[0];
            if (lyric.TryGetProperty("attributes", out var attributes) &&
                attributes.TryGetProperty("ttml", out var ttml))
            {
                return ttml.GetString();
            }
        }

        return null;
    }

    public async Task<LyricsCacheItem> SearchSongInfoAsync(SongInfo songInfo, CancellationToken token)
    {
        LyricsCacheItem lyricsSearchResult = new()
        {
            Provider = LyricsProvider.AppleMusic
        };

        var query = $"{songInfo.Artist} {songInfo.Title}";
        var apiUrl = $"https://amp-api.music.apple.com/v1/catalog/{_storefront}/search";
        var url = apiUrl + $"?term={WebUtility.UrlEncode(query)}&types=songs&limit=1&l={_language}";
        var resp = await _client.GetStringAsync(url, token);
        var json = JsonSerializer.Deserialize(resp, SourceGenerationContext.Default.JsonElement);
        var results = json.GetProperty("results");
        if (results.TryGetProperty("songs", out var songs) && songs.GetProperty("data").GetArrayLength() > 0)
        {
            var song = songs.GetProperty("data")[0];

            var id = song.GetProperty("id").ToString();

            var attr = song.GetProperty("attributes");

            lyricsSearchResult.Title = attr.GetProperty("name").ToString();
            lyricsSearchResult.Artist = attr.GetProperty("artistName").ToString();
            lyricsSearchResult.Album = attr.GetProperty("albumName").ToString();
            lyricsSearchResult.Duration = attr.GetProperty("durationInMillis").GetInt32() / 1000.0;

            lyricsSearchResult.Reference = $"https://music.apple.com/song/{id}";
            lyricsSearchResult.MatchPercentage = MetadataComparer.CalculateScore(songInfo, lyricsSearchResult);

            if (id != null) lyricsSearchResult.Raw = await GetLyricsAsync(id, token);
        }

        return lyricsSearchResult;
    }
}