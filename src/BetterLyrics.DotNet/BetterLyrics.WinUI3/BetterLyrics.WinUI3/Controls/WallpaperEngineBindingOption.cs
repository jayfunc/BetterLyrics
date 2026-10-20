namespace BetterLyrics.WinUI3.Controls;

public sealed class WallpaperEngineBindingOption
{
    public string? ProjectPath { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public override string ToString() => DisplayName;
}
