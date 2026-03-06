using Newtonsoft.Json;

#nullable disable
namespace Lyricify.Lyrics.Parsers.Models
{
    public class RichSyncedLine
    {
        [JsonProperty("ts")]
        public float TimeStart { get; set; }

        [JsonProperty("te")]
        public float TimeEnd { get; set; }

        [JsonProperty("l")]
        public List<Word> Words { get; set; }

        public class Word
        {
            [JsonProperty("c")]
            public string Chars { get; set; }

            [JsonProperty("o")]
            public double Position { get; set; }
        }

        [JsonProperty("x")]
        public string Text { get; set; }
    }
}
