using Newtonsoft.Json;

#nullable disable
namespace Lyricify.Lyrics.Parsers.Models.Yrc
{
    public class CreditsInfo
    {
        [JsonProperty("t")]
        public int Timestamp { get; set; }

        [JsonProperty("c")]
        public List<Credit> Credits { get; set; }

        public class Credit
        {
            [JsonProperty("tx")]
            public string Text { get; set; }

            [JsonProperty("li")]
            public string Image { get; set; }

            [JsonProperty("or")]
            public string Orpheus { get; set; }
        }
    }

}
