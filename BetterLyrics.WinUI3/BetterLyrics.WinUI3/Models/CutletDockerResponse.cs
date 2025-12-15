using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace BetterLyrics.WinUI3.Models
{
    public class CutletDockerResponse
    {
        [JsonPropertyName("romaji")]
        public string RomajiText { get; set; }
    }
}
