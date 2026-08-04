using System.Net;
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

public class LegacyAppleMusic
{
    private readonly HttpClient _client;

    private readonly IPasswordVaultProvider _passwordVaultProvider =
        Ioc.Default.GetRequiredService<IPasswordVaultProvider>();

    private string _accessToken = "";
    private bool _isInited;
    private string _language = "";
    private string _storefront = "";

    public LegacyAppleMusic()
    {
        _client = new HttpClient();
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
        var jsMatch = Regex.Match(resp, "(?<=index)(.*?)(?=\\.js\")");
        if (!jsMatch.Success) throw new Exception("Failed to find index.js");
        var jsUrl = $"https://music.apple.com/assets/index{jsMatch.Value}.js";
        var jsResp = await _client.GetStringAsync(jsUrl);
        var tokenMatch = Regex.Match(jsResp, "(?=eyJh)(.*?)(?=\")");
        if (!tokenMatch.Success) throw new Exception("Failed to find access token");
        _accessToken = tokenMatch.Value;
        _client.DefaultRequestHeaders.Remove("Authorization");
        _client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_accessToken}");
    }

    private async Task SetMediaUserTokenAsync(string token, CancellationToken cancellationToken)
    {
        _client.DefaultRequestHeaders.Remove("media-user-token");
        _client.DefaultRequestHeaders.Add("media-user-token", token);
        var resp = await _client.GetStringAsync("https://amp-api.music.apple.com/v1/me/storefront",
            cancellationToken);
        var json = JsonSerializer.Deserialize(resp, SourceGenerationContext.Default.JsonElement);
        _storefront = json.GetProperty("data")[0].GetProperty("id").ToString();
        _language = json.GetProperty("data")[0].GetProperty("attributes").GetProperty("defaultLanguageTag")
            .ToString();
        _client.DefaultRequestHeaders.Remove("Accept-Language");
        _client.DefaultRequestHeaders.Add("Accept-Language", $"{_language},en;q=0.9");
    }

    private async Task<string?> GetLyricsAsync(string id, CancellationToken token)
    {
        var apiUrl = $"https://amp-api.music.apple.com/v1/catalog/{_storefront}/songs/{id}";
        var url = apiUrl + $"?include[songs]=lyrics,syllable-lyrics&l={_language}";
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

        //if (relationships.TryGetProperty("lyrics", out var lyrics) &&
        //    lyrics.GetProperty("data").GetArrayLength() > 0)
        //{
        //    var lyric = lyrics.GetProperty("data")[0];
        //    if (lyric.TryGetProperty("attributes", out var attributes) &&
        //        attributes.TryGetProperty("ttml", out var ttml))
        //    {
        //        return ttml.GetString();
        //    }
        //}

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