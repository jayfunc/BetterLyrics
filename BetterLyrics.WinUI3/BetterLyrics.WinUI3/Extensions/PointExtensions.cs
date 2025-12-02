using Windows.Foundation;
using Windows.Graphics;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class PointExtensions
    {
        extension(Point point)
        {
            public PointInt32 ToPointInt32() => new((int)point.X, (int)point.Y);

            public Point AddX(double deltaX) => new(point.X + deltaX, point.Y);
            public Point AddY(double deltaY) => new(point.X, point.Y + deltaY);
        }
    }
}
