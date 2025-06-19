using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;

namespace BetterLyrics.WinUI3.Services
{
    public class LrcLibService : ILrcLibService
    {
        private static readonly HttpClient _httpClient;

        static LrcLibService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add(
                "User-Agent",
                $"{AppInfo.AppName} {AppInfo.AppVersion} ({AppInfo.GithubUrl})"
            );
        }

        public async Task<string?> SearchLyricsAsync(
            string title,
            string artist,
            string album,
            int duration,
            SearchMatchMode matchMode = SearchMatchMode.TitleAndArtist
        )
        {
            // Build API query URL
            var url =
                $"https://lrclib.net/api/search?"
                + $"track_name={Uri.EscapeDataString(title)}&"
                + $"artist_name={Uri.EscapeDataString(artist)}";

            if (matchMode == SearchMatchMode.TitleArtistAlbumAndDuration)
            {
                url +=
                    $"&album_name={Uri.EscapeDataString(album)}"
                    + $"&duration={Uri.EscapeDataString(duration.ToString())}";
            }

            var response = await _httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            var jObj = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);
            var jArr = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(json);
            if (jArr is not null && jArr.Count > 0)
            {
                var syncedLyrics = jArr![0]?.syncedLyrics?.ToString();
                return string.IsNullOrWhiteSpace(syncedLyrics) ? null : syncedLyrics;
            }
            return null;
        }
    }
}
