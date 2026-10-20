using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Interfaces.Services;

public interface ILyricsWindowVisibilityService
{
    /// <summary>
    /// Evaluates every lyrics window after startup and applies the combined
    /// wallpaper, media, monitor and user visibility conditions.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Re-evaluates one window after its wallpaper binding changed.
    /// </summary>
    void NotifyBindingChanged(LyricsWindowStatus status);

    /// <summary>
    /// Re-evaluates every window immediately after the active Wallpaper Engine
    /// wallpaper changed.
    /// </summary>
    void NotifyWallpaperChanged();

    /// <summary>
    /// Applies the policy after a window's root element has loaded.
    /// </summary>
    void NotifyWindowLoaded(LyricsWindowStatus status);
}
