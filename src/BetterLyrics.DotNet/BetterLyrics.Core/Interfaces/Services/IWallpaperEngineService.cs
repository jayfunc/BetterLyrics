using BetterLyrics.Core.Models.WallpaperEngine;

namespace BetterLyrics.Core.Interfaces.Services;

public interface IWallpaperEngineService
{
    /// <summary>
    /// Whether Wallpaper Engine is installed (its config file can be found).
    /// </summary>
    bool IsAvailable { get; }

    Task InitializeAsync();

    /// <summary>
    /// Project.json paths of the wallpapers currently assigned to monitors, or
    /// an empty set when Wallpaper Engine is not running. Returns
    /// <see langword="null"/> when the state is unknown.
    /// </summary>
    IReadOnlySet<string>? GetActiveWallpaperProjectPaths();

    Task<IReadOnlyList<WallpaperEngineWallpaper>> GetInstalledWallpapersAsync(bool forceRefresh = false,
        CancellationToken cancellationToken = default);

    event EventHandler<WallpaperEngineActiveWallpaperChangedEventArgs>? ActiveWallpaperChanged;
}
