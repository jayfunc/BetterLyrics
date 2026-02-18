using BetterLyrics.Core.Abstractions;
using BetterLyrics.Core.Interfaces.Features;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Serialization;
using System.Text.Json;

namespace BetterLyrics.Plugins.Source.LRCLIB
{
    public class Plugin : PluginBase<Config>, ILyricsSource
    {
        private readonly HttpClient _httpClient;

        public override string Title { get; set; } = "LRCLIB";

        public Plugin()
        {
            _httpClient = new HttpClient();
        }

        protected override Task OnInitializeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task<LyricsSearchResult> GetLyricsAsync(string title, string artist, string album, double duration, CancellationToken token)
        {
            var url =
                $"https://lrclib.net/api/search?" +
                $"track_name={Uri.EscapeDataString(title)}&" +
                $"artist_name={Uri.EscapeDataString(artist)}&" +
                $"&album_name={Uri.EscapeDataString(album)}" +
                $"&durationMs={Uri.EscapeDataString((duration * 1000).ToString())}";

            using var response = await _httpClient.GetAsync(url, token);

            var json = await response.Content.ReadAsStringAsync(token);
            var jArr = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.JsonElement);

            string? original = null;
            string? searchedTitle = null;
            string? searchedArtist = null;
            string? searchedAlbum = null;
            double? searchedDuration = null;

            if (jArr.ValueKind == JsonValueKind.Array && jArr.GetArrayLength() > 0)
            {
                var first = jArr[0];
                original = first.GetProperty("syncedLyrics").GetString();
                searchedTitle = first.GetProperty("trackName").GetString();
                searchedArtist = first.GetProperty("artistName").GetString();
                searchedAlbum = first.GetProperty("albumName").GetString();
                searchedDuration = first.GetProperty("duration").GetDouble();
            }

            return new LyricsSearchResult(searchedTitle, searchedArtist, searchedAlbum, searchedDuration, original, null, null, url);
        }
    }
}
