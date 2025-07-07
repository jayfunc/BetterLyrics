using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models
{
    public class DetectLanguageResult
    {
        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; }
    }
}
