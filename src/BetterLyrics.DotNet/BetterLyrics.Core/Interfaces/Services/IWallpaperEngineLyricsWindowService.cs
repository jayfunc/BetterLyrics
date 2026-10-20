namespace BetterLyrics.Core.Interfaces.Services;

public interface IWallpaperEngineLyricsWindowService
{
    /// <summary>
    /// Initializes the shared lyrics-window visibility policy and applies the
    /// current Wallpaper Engine wallpaper. Call once after the windows restored
    /// at startup have been created.
    /// </summary>
    void Initialize();
}
