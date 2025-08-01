using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.UI;

namespace BetterLyrics.WinUI3.Events
{
    public class AlbumArtChangedEventArgs(SoftwareBitmap? albumArtSwBitmap, Color? albumArtLightAccentColor, Color? albumArtDarkAccentColor) : EventArgs
    {
        public SoftwareBitmap? AlbumArtSwBitmap { get; set; } = albumArtSwBitmap;
        public Color? AlbumArtLightAccentColor { get; set; } = albumArtLightAccentColor;
        public Color? AlbumArtDarkAccentColor { get; set; } = albumArtDarkAccentColor;
    }
}
