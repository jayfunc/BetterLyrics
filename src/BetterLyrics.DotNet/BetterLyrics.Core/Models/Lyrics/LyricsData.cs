using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Enums;
using NLanguageTag;

namespace BetterLyrics.Core.Models.Lyrics;

public class LyricsData
{
    public LyricsData()
    {
    }

    public LyricsData(List<LyricsLine> lyricsLines)
    {
        LyricsLines = lyricsLines;
    }

    public List<LyricsLine> LyricsLines { get; set; } = [];

    public LanguageTag? LanguageTag
    {
        get => field ?? LanguageHelper.DetectLanguageTag(LyricsLines.Select(line => line.PrimaryText));
        set;
    }

    public LyricsProvider? Provider { get; set; }
    public bool HasProvider => Provider != null;
    public bool IsProviderSameAsOriginal { get; set; }

    public LyricsTrackType TrackType { get; set; } = LyricsTrackType.Original;

    public string WrappedPrimaryText =>
        string.Join(StringHelper.NewLine, LyricsLines.Select(line => line.PrimaryText));

    public bool IsWordByWord => LyricsLines.Any(x => x.IsPrimaryHasRealSyllableInfo);
}