using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;

namespace BetterLyrics.WinUI3.Helper
{
    public static class PointHelper
    {
        public static PointInt32 ToPointInt32(this Point point)
        {
            return new PointInt32((int)point.X, (int)point.Y);
        }
    }
}
