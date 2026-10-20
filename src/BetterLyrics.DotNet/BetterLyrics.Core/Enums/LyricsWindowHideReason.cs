namespace BetterLyrics.Core.Enums;

/// <summary>
/// Conditions that can independently keep a lyrics window hidden.
/// A window is visible only when this set is empty.
/// </summary>
[Flags]
public enum LyricsWindowHideReason
{
    None = 0,
    UserHidden = 1 << 0,
    WallpaperMismatch = 1 << 1,
    MediaPaused = 1 << 2,
    NoMediaSession = 1 << 3,
    MonitorDisconnected = 1 << 4
}
