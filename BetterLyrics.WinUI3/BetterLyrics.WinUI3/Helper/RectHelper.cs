using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics;

namespace BetterLyrics.WinUI3.Helper
{
    public static class RectHelper
    {
        public static RectInt32 ToRectInt32(this Windows.Foundation.Rect rect)
        {
            return new RectInt32(
                (int)rect.X,
                (int)rect.Y,
                (int)rect.Width,
                (int)rect.Height
            );
        }
    }
}
