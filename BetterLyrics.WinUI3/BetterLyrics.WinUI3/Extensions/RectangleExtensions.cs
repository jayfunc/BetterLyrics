using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class RectangleExtensions
    {
        extension(Rectangle rect)
        {
            public Rect ToRect() => new(
                rect.Left,
                rect.Top,
                rect.Right - rect.Left,
                rect.Bottom - rect.Top
            );
        }
    }
}
