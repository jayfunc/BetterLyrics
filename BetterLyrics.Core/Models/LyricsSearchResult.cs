using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.Core.Models
{
    public record LyricsSearchResult(
        string? Title, 
        string? Artist, 
        string? Album, 
        double? Duration, 
        string Raw, 
        string? Translation = null, 
        string? Transliteration = null,
        string? Reference = null);
}
