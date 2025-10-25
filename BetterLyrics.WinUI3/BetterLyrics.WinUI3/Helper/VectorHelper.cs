using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    public static class VectorHelper
    {
        public static Vector2 WithX(this Vector2 source, float x)
        {
            return new Vector2(x, source.Y);
        }

        public static Vector2 WithY(this Vector2 source, float y)
        {
            return new Vector2(source.X, y);
        }
    }
}
