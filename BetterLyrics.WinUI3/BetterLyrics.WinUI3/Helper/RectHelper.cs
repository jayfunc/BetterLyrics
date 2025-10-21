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

        public static Windows.Foundation.Rect WithHeight(this Windows.Foundation.Rect rect, double height)
        {
            return new Windows.Foundation.Rect(
                rect.X,
                rect.Y,
                rect.Width,
                height
            );
        }

        public static Windows.Foundation.Rect WithWidth(this Windows.Foundation.Rect rect, double width)
        {
            return new Windows.Foundation.Rect(
                rect.X,
                rect.Y,
                width,
                rect.Height
            );
        }

        public static Windows.Foundation.Rect WithX(this Windows.Foundation.Rect rect, double x)
        {
            return new Windows.Foundation.Rect(
                x,
                rect.Y,
                rect.Width,
                rect.Height
            );
        }

        public static Windows.Foundation.Rect WithY(this Windows.Foundation.Rect rect, double y)
        {
            return new Windows.Foundation.Rect(
                rect.X,
                y,
                rect.Width,
                rect.Height
            );
        }
    }
}
