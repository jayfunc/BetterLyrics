namespace BetterLyrics.Core.Enums;

public enum NowPlayingLayoutMode
{
    LyricsOnly, // Desktop / Wallpaper
    AlbumArtOnly,
    LeftAlbumArtRightLyrics, // Standard
    LeftLyricsRightAlbumArt, // Standard
    LeftAlbumArtRightLyricsCompact, // Taskbar / Docked
    LeftLyricsRightAlbumArtCompact, // Taskbar / Docked
    TopAlbumArtBottomLyrics, // Fullscreen
    TopAlbumArtBottomLyricsCompact, // Narrow
    LyricsCardOnly,
    Custom = 999
}