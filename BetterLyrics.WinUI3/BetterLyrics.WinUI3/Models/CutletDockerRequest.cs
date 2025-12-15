using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace BetterLyrics.WinUI3.Models
{
    public class CutletDockerRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; set; }
    }
}
