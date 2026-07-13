namespace BetterLyrics.Core.Models.Lyrics;

public record LyricsSearchResult(
    string? Title,
    string? Artist,
    string? Album,
    double? Duration,
    string? Raw,
    string? Translation = null,
    string? Transliteration = null,
    string? Reference = null);