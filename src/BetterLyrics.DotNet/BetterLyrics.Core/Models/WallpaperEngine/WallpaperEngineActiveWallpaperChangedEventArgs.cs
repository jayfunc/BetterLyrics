namespace BetterLyrics.Core.Models.WallpaperEngine;

public sealed class WallpaperEngineActiveWallpaperChangedEventArgs : EventArgs
{
    public WallpaperEngineActiveWallpaperChangedEventArgs(IReadOnlySet<string> activeWallpaperProjectPaths)
    {
        ActiveWallpaperProjectPaths = activeWallpaperProjectPaths;
    }

    /// <summary>
    /// Project.json paths of all wallpapers currently assigned to a monitor.
    /// </summary>
    public IReadOnlySet<string> ActiveWallpaperProjectPaths { get; }
}
