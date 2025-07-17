using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.UI;

namespace BetterLyrics.WinUI3.Events
{
    public class AlbumArtChangedEventArgs(SoftwareBitmap? albumArtSwBitmap, Color? albumArtAccentColor) : EventArgs
    {
        public SoftwareBitmap? AlbumArtSwBitmap { get; set; } = albumArtSwBitmap;
        public Color? AlbumArtAccentColor { get; set; } = albumArtAccentColor;
    }
}
