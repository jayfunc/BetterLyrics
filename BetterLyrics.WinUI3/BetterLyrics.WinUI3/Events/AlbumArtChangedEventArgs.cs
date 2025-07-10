using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.UI;

namespace BetterLyrics.WinUI3.Events
{
    public class AlbumArtChangedEventArgs : EventArgs
    {
        public SoftwareBitmap? AlbumArtSwBitmap { get; set; } = null;
        public Color? AlbumArtAccentColor { get; set; } = null;
    }
}
