namespace BetterLyrics.Core.Models.WallpaperEngine;

/// <summary>
/// A wallpaper project installed in Wallpaper Engine.
/// </summary>
public sealed class WallpaperEngineWallpaper
{
    /// <summary>
    /// Full path to the project.json file that identifies the wallpaper.
    /// </summary>
    public required string ProjectPath { get; init; }

    /// <summary>
    /// Title stored in project.json.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Display name shown in the binding picker. Duplicate titles are disambiguated.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Path to the preview image stored next to project.json, or
    /// <see langword="null"/> when the wallpaper has no preview image.
    /// </summary>
    public string? PreviewImagePath { get; init; }

    public string? WorkshopId { get; init; }

    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// Stable short identifier used to disambiguate duplicate titles.
    /// </summary>
    public string Identifier =>
        !string.IsNullOrEmpty(WorkshopId)
            ? WorkshopId
            : Path.GetFileName(Path.GetDirectoryName(ProjectPath)) ?? ProjectPath;
}
