namespace BetterLyrics.Core.Models;

public sealed class WallpaperEngineBindingOption
{
    public string? ProjectPath { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Plain wallpaper name without disambiguation or status suffixes, used when copying.
    /// </summary>
    public string CopyName { get; set; } = string.Empty;

    public string? PreviewImagePath { get; set; }

    public override string ToString() => DisplayName;
}
