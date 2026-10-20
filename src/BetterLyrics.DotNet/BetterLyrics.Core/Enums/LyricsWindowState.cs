namespace BetterLyrics.Core.Enums;

/// <summary>
/// Lifecycle/visibility state of a lyrics window. Causes of hiding are kept
/// separately in <see cref="LyricsWindowHideReason"/>.
/// </summary>
public enum LyricsWindowState
{
    Closed = 0,
    Opened,
    Hidden
}
