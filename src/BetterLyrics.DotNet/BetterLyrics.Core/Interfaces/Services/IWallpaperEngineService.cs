using BetterLyrics.Core.Models.WallpaperEngine;

namespace BetterLyrics.Core.Interfaces.Services;

public interface IWallpaperEngineService
{
    bool IsAvailable { get; }

    Task InitializeAsync();

    /// <summary>
    /// Project.json paths of the wallpapers currently assigned to monitors,
    /// or <see langword="null"/> when the state is unknown.
    /// </summary>
    IReadOnlySet<string>? GetActiveWallpaperProjectPaths();

    Task<IReadOnlyList<WallpaperEngineWallpaper>> GetInstalledWallpapersAsync(bool forceRefresh = false,
        CancellationToken cancellationToken = default);

    event EventHandler<WallpaperEngineActiveWallpaperChangedEventArgs>? ActiveWallpaperChanged;
}
