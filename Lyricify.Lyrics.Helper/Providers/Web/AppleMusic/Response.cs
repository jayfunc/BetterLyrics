#nullable disable
using Newtonsoft.Json;

namespace Lyricify.Lyrics.Providers.Web.AppleMusic
{
    // ===== /v1/me/storefront =====
    public class StorefrontResponse
    {
        [JsonProperty("data")]
        public StorefrontData[] Data { get; set; }
    }

    public class StorefrontData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("attributes")]
        public StorefrontAttributes Attributes { get; set; }
    }

    public class StorefrontAttributes
    {
        [JsonProperty("defaultLanguageTag")]
        public string DefaultLanguageTag { get; set; }
    }

    // ===== /v1/catalog/{storefront}/search =====
    public class SearchResponse
    {
        [JsonProperty("results")]
        public SearchResults Results { get; set; }
    }

    public class SearchResults
    {
        [JsonProperty("songs")]
        public SongsContainer Songs { get; set; }
    }

    public class SongsContainer
    {
        [JsonProperty("data")]
        public SongData[] Data { get; set; }
    }

    public class SongData
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("attributes")]
        public SongAttributes Attributes { get; set; }
    }

    public class SongAttributes
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("artistName")]
        public string ArtistName { get; set; }

        [JsonProperty("albumName")]
        public string AlbumName { get; set; }

        [JsonProperty("durationInMillis")]
        public int DurationInMillis { get; set; }
    }

    public class LyricResponse
    {
        [JsonProperty("data")]
        public LyricSongData[] Data { get; set; }

        /// <summary>
        /// 归一化输出
        /// </summary>
        [JsonIgnore]
        public string Ttml { get; private set; }

        public void NormalizeTtml()
        {
            Ttml = null;

            if (Data == null || Data.Length == 0)
                return;

            var song = Data[0];
            var rel = song?.Relationships;
            if (rel == null)
                return;

            var syll = rel.SyllableLyrics;
            if (syll?.Data == null || syll.Data.Length == 0)
                return;

            var attr = syll.Data[0]?.Attributes;
            if (attr == null)
                return;

            // 1) 优先使用 ttmlLocalizations
            var ttml = attr.TtmlLocalizations;

            // 2) 回退到 ttml
            if (string.IsNullOrWhiteSpace(ttml))
                ttml = attr.Ttml;

            // 3) 校验是否包含时间轴
            if (!string.IsNullOrWhiteSpace(ttml)
                && ttml.Contains("begin=")
                && ttml.Contains("end="))
            {
                Ttml = ttml;
            }
        }
    }

    public class LyricSongData
    {
        [JsonProperty("relationships")]
        public LyricRelationships Relationships { get; set; }
    }

    public class LyricRelationships
    {
        [JsonProperty("syllable-lyrics")]
        public LyricContainer SyllableLyrics { get; set; }

        [JsonProperty("lyrics")]
        public LyricContainer Lyrics { get; set; }
    }

    public class LyricContainer
    {
        [JsonProperty("data")]
        public LyricData[] Data { get; set; }
    }

    public class LyricData
    {
        [JsonProperty("attributes")]
        public LyricAttributes Attributes { get; set; }
    }

    public class LyricAttributes
    {
        [JsonProperty("ttml")]
        public string Ttml { get; set; }

        [JsonProperty("ttmlLocalizations")]
        public string TtmlLocalizations { get; set; }
    }
}
