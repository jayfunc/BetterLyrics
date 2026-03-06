using Newtonsoft.Json;

#nullable disable
namespace Lyricify.Lyrics.Parsers.Models.Spotify
{
    public class SpotifyColorLyrics
    {
        [JsonProperty("lyrics")]
        public SpotifyLyrics Lyrics { get; set; }

        [JsonProperty("colors")]
        public SpotifyColors Colors { get; set; }

        [JsonProperty("hasVocalRemoval")]
        public bool HasVocalRemoval { get; set; }
    }

    public class SpotifyLyrics
    {
        [JsonProperty("syncType")]
        public string SyncType { get; set; }

        [JsonProperty("lines")]
        public List<SpotifyLyricsLine> Lines { get; set; }

        [JsonProperty("provider")]
        public string Provider { get; set; }

        [JsonProperty("providerLyricsId")]
        public string ProviderLyricsId { get; set; }

        [JsonProperty("providerDisplayName")]
        public string ProviderDisplayName { get; set; }

        [JsonProperty("syncLyricsUri")]
        public string SyncLyricsUri { get; set; }

        [JsonProperty("isDenseTypeface")]
        public bool IsDenseTypeface { get; set; }

        [JsonProperty("alternatives")]
        public List<AlternativeItem> Alternatives { get; set; }

        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("isRtlLanguage")]
        public bool IsRtlLanguage { get; set; }

        [JsonProperty("fullscreenAction")]
        public string FullscreenAction { get; set; }
    }

    public class SpotifyLyricsLine
    {
        [JsonProperty("startTimeMs")]
        public string StartTimeMs { get; set; }

        public int StartTime
        {
            get
            {
                try
                {
                    return int.Parse(StartTimeMs);
                }
                catch
                {
                    return 0;
                }
            }
        }

        [JsonProperty("endTimeMs")]
        public string EndTimeMs { get; set; }

        public int EndTime
        {
            get
            {
                try
                {
                    return int.Parse(EndTimeMs);
                }
                catch
                {
                    return 0;
                }
            }
        }

        [JsonProperty("words")]
        public string Words { get; set; }

        [JsonProperty("syllables")]
        public List<SyllableItem> Syllables { get; set; }
    }

    public class SyllableItem
    {
        [JsonProperty("startTimeMs")]
        public string StartTimeMs { get; set; }

        public int StartTime
        {
            get
            {
                try
                {
                    return int.Parse(StartTimeMs);
                }
                catch
                {
                    return 0;
                }
            }
        }

        [JsonProperty("endTimeMs")]
        public string EndTimeMs { get; set; }

        public int EndTime
        {
            get
            {
                try
                {
                    return int.Parse(EndTimeMs);
                }
                catch
                {
                    return 0;
                }
            }
        }

        [JsonProperty("numChars")]
        public string NumberChars { get; set; }

        public int CharsCount
        {
            get
            {
                try
                {
                    return int.Parse(NumberChars);
                }
                catch
                {
                    return 0;
                }
            }
        }
    }

    public class AlternativeItem
    {
        [JsonProperty("language")]
        public string Language { get; set; }

        [JsonProperty("lines")]
        public List<string> Lines { get; set; }

        [JsonProperty("isRtlLanguage")]
        public bool IsRtlLanguage { get; set; }
    }

    public class SpotifyColors
    {
        [JsonProperty("background")]
        public int Background { get; set; }

        [JsonProperty("text")]
        public int Text { get; set; }

        [JsonProperty("highlightText")]
        public int HighlightText { get; set; }
    }
}
