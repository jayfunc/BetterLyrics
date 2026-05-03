using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Enums
{
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
        Custom = 999,
    }
}
