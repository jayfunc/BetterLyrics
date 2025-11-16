using System;
using System.Collections.Generic;
using Windows.Graphics.Imaging;
using Windows.UI;

namespace BetterLyrics.WinUI3.Events
{
    public class AlbumArtChangedEventArgs(SoftwareBitmap? albumArtSwBitmap, List<Color> albumArtLightAccentColors, List<Color> albumArtDarkAccentColors) : EventArgs
    {
        public SoftwareBitmap? AlbumArtSwBitmap { get; set; } = albumArtSwBitmap;
        public List<Color> AlbumArtLightAccentColors { get; set; } = albumArtLightAccentColors;
        public List<Color> AlbumArtDarkAccentColors { get; set; } = albumArtDarkAccentColors;
    }
}
