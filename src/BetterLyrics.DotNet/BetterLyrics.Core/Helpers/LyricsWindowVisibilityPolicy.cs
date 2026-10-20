using BetterLyrics.Core.Enums;

namespace BetterLyrics.Core.Helpers;

/// <summary>
/// Combines every independent visibility condition of a lyrics window.
/// Wallpaper binding and playback auto-hide coexist: hiding wins if any
/// condition asks for it, and the window is shown only when all allow it.
/// </summary>
public static class LyricsWindowVisibilityPolicy
{
    public static LyricsWindowHideReason GetHideReasons(
        bool isWallpaperBound,
        bool isWallpaperMatched,
        bool hideWindowWhenPaused,
        bool isPlaying,
        bool hideWindowWhenNullSession,
        bool hasMediaSession,
        bool isTargetMonitorConnected,
        bool isUserHidden)
    {
        var reasons = LyricsWindowHideReason.None;

        if (isUserHidden) reasons |= LyricsWindowHideReason.UserHidden;
        if (isWallpaperBound && !isWallpaperMatched) reasons |= LyricsWindowHideReason.WallpaperMismatch;
        if (hideWindowWhenPaused && !isPlaying) reasons |= LyricsWindowHideReason.MediaPaused;
        if (hideWindowWhenNullSession && !hasMediaSession) reasons |= LyricsWindowHideReason.NoMediaSession;
        if (!isTargetMonitorConnected) reasons |= LyricsWindowHideReason.MonitorDisconnected;

        return reasons;
    }

    public static bool ShouldShow(LyricsWindowHideReason reasons) => reasons == LyricsWindowHideReason.None;
}
