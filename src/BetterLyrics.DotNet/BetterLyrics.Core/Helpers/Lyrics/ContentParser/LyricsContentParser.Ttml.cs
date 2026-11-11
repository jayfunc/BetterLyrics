using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers.Lyrics.Ttml;
using BetterLyrics.Core.Models.Lyrics;
using NLanguageTag;
using System.Xml;

namespace BetterLyrics.Core.Helpers.Lyrics.ContentParser;

public partial class LyricsContentParser
{
    private void ParseTtml(string raw, LyricsProvider? provider, LyricsParseRule rule)
    {
        try
        {
            var document = TtmlParser.Parse(raw);
            if (document.Metadata.TimingMode == "None") return;

            List<LyricsLine> originalLines = [];
            Dictionary<string, List<LyricsLine>> translations = [];
            Dictionary<string, List<LyricsLine>> romanizations = [];
            foreach (var line in document.Lines)
            {
                AddVocal(line, line.AgentId);
                if (line.BackgroundVocal is { } bg) AddVocal(bg, line.AgentId);
            }
            var original = new LyricsData(originalLines)
            {
                Provider = provider,
                LanguageTag = ParseLanguage(document.Metadata.Language)
            };
            List<LyricsData> tracks = [original];
            AddTracks(translations, LyricsTrackType.Translation);
            AddTracks(romanizations, LyricsTrackType.Transliteration);
            foreach (var track in tracks) AddLyricsData(track);

            void AddVocal(TtmlVocal vocal, string? agent)
            {
                if (!string.IsNullOrWhiteSpace(vocal.Text))
                    originalLines.Add(TtmlLyricsAdapter.ToLine(vocal, vocal.StartTime, vocal.EndTime, agent));
                AddAuxiliary(vocal.Translations, translations, false);
                AddAuxiliary(vocal.Romanizations, romanizations, true);

                void AddAuxiliary(List<TtmlSubLyric>? contents, Dictionary<string, List<LyricsLine>> destination, bool roman)
                {
                    foreach (var content in contents ?? [])
                    {
                        if (string.IsNullOrWhiteSpace(content.Text)) continue;
                        var language = content.Language ?? "";
                        if (!destination.TryGetValue(language, out var lines)) destination[language] = lines = [];
                        lines.Add(TtmlLyricsAdapter.ToLine(content, vocal.StartTime, vocal.EndTime, agent, roman));
                    }
                }
            }

            void AddTracks(Dictionary<string, List<LyricsLine>> source, LyricsTrackType type)
            {
                foreach (var (language, lines) in source)
                {
                    var tag = ParseLanguage(language);
                    if (type == LyricsTrackType.Transliteration)
                        tag = LanguageHelper.GetPhoneticTag(tag ?? original.LanguageTag);
                    var allowed = type == LyricsTrackType.Translation
                        ? rule.IsTranslationEnabled && rule.IsTranslationAllowed(tag)
                        : rule.IsRomanizationAllowed(tag, original.LanguageTag);
                    if (allowed) tracks.Add(new LyricsData(lines) { Provider = provider, TrackType = type, LanguageTag = tag });
                }
            }
        }
        catch (Exception ex) when (ex is XmlException or FormatException or OverflowException)
        {
            System.Diagnostics.Debug.WriteLine($"TTML parsing failed: {ex.Message}");
        }
    }

    private static LanguageTag? ParseLanguage(string? language) =>
        string.IsNullOrWhiteSpace(language) ? null :
        LanguageTag.TryParse(language, out var tag) ? tag : null;
}
