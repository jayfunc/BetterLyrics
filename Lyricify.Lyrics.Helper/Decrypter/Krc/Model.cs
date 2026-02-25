using Newtonsoft.Json;

namespace Lyricify.Lyrics.Decrypter.Krc
{
    public class KugouLyricsResponse
    {
        [JsonProperty("content")]
        public string? Content { get; set; }

        [JsonProperty("info")]
        public string? Info { get; set; }

        [JsonProperty("_source")]
        public string? Source { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("contenttype")]
        public int ContentType { get; set; }

        [JsonProperty("error_code")]
        public int ErrorCode { get; set; }

        [JsonProperty("fmt")]
        public string? Format { get; set; }
    }

    public class KugouTranslation
    {
        [JsonProperty("content")]
        public List<ContentItem>? Content { get; set; }

        public class ContentItem
        {
            [JsonProperty("language")]
            public int Language { get; set; }

            [JsonProperty("type")]
            public int Type { get; set; }

            [JsonProperty("lyricContent")]
            public List<List<string>?>? LyricContent { get; set; }
        }

        [JsonProperty("version")]
        public int Version { get; set; }
    }
}