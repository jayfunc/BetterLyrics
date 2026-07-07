using Avalonia;
using System.Numerics;

namespace BetterLyrics.Avalonia.Extensions;

public static class Vector2Extensions
{
    extension(Vector2 vector2)
    {
        public Point ToPoint()
        {
            return new Point(vector2.X, vector2.Y);
        }
    }
}
