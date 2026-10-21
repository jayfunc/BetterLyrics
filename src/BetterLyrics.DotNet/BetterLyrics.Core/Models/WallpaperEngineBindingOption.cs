namespace BetterLyrics.Core.Models;

public sealed class WallpaperEngineBindingOption
{
    public string? ProjectPath { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public override string ToString() => DisplayName;
}
