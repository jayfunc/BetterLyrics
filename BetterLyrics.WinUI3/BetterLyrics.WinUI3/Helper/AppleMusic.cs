using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    public class AppleMusic
    {
        private readonly HttpClient _client;
        private string _accessToken = "";
        private string _storefront = "";
        private string _language = "";

        public AppleMusic()
        {
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/110.0.0.0 Safari/537.36");
            _client.DefaultRequestHeaders.Add("Accept", "application/json");
            _client.DefaultRequestHeaders.Add("Origin", "https://music.apple.com");
            _client.DefaultRequestHeaders.Add("Referer", "https://music.apple.com/");
        }

        public async Task<bool> InitAsync()
        {
            await GetAccessTokenAsync();
            await SetMediaUserTokenAsync();
            return
                !string.IsNullOrEmpty(_accessToken) &&
                !string.IsNullOrEmpty(PasswordVaultHelper.Get(Constants.App.AppName, Constants.AppleMusic.MediaUserTokenKey));
        }

        private async Task GetAccessTokenAsync()
        {
            var resp = await _client.GetStringAsync("https://music.apple.com/us/browse");
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

        private async Task SetMediaUserTokenAsync()
        {
            _client.DefaultRequestHeaders.Remove("media-user-token");
            _client.DefaultRequestHeaders.Add("media-user-token",
                PasswordVaultHelper.Get(Constants.App.AppName, Constants.AppleMusic.MediaUserTokenKey));
            var resp = await _client.GetStringAsync("https://amp-api.music.apple.com/v1/me/storefront");
            var json = JsonSerializer.Deserialize(resp, Serialization.SourceGenerationContext.Default.JsonElement);
            _storefront = json.GetProperty("data")[0].GetProperty("id").ToString();
            _language = json.GetProperty("data")[0].GetProperty("attributes").GetProperty("defaultLanguageTag").ToString();
            _client.DefaultRequestHeaders.Remove("Accept-Language");
            _client.DefaultRequestHeaders.Add("Accept-Language", $"{_language},en;q=0.9");
        }

        public async Task<string?> GetLyricsAsync(string title, string artist)
        {
            string id = await SearchSongInfoAsync(artist, title);

            var apiUrl = $"https://amp-api.music.apple.com/v1/catalog/{_storefront}/songs/{id}";
            var url = apiUrl + $"?include[songs]=lyrics,syllable-lyrics&l={_language}";
            var resp = await _client.GetStringAsync(url);
            var json = JsonSerializer.Deserialize(resp, Serialization.SourceGenerationContext.Default.JsonElement);
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
                    string? raw = ttml.GetString();
                    if (raw != null && raw.Contains("begin=") && raw.Contains("end="))
                    {
                        return raw;
                    }
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

        private async Task<string> SearchSongInfoAsync(string artist, string title)
        {
            var query = $"{artist} {title}";
            var apiUrl = $"https://amp-api.music.apple.com/v1/catalog/{_storefront}/search";
            var url = apiUrl + $"?term={WebUtility.UrlEncode(query)}&types=songs&limit=1&l={_language}";
            var resp = await _client.GetStringAsync(url);
            var json = JsonSerializer.Deserialize(resp, Serialization.SourceGenerationContext.Default.JsonElement);
            var results = json.GetProperty("results");
            if (results.TryGetProperty("songs", out var songs) && songs.GetProperty("data").GetArrayLength() > 0)
            {
                var song = songs.GetProperty("data")[0];
                return song.GetProperty("id").ToString();
            }
            return string.Empty;
        }
    }
}
