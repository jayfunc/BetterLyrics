using BetterLyrics.Core.Models.Domain;
using Windows.Foundation;
using Windows.Graphics;

namespace BetterLyrics.WinUI3.Extensions;

public static class PointExtensions
{
    extension(Point point)
    {
        public PointInt32 ToPointInt32()
        {
            return new PointInt32((int)point.X, (int)point.Y);
        }

        public Point AddX(double deltaX)
        {
            return new Point(point.X + deltaX, point.Y);
        }

        public Point AddY(double deltaY)
        {
            return new Point(point.X, point.Y + deltaY);
        }

        public Point WithX(double x)
        {
            return new Point(x, point.Y);
        }

        public Point WithY(double y)
        {
            return new Point(point.X, y);
        }

        public AppPoint ToAppPoint()
        {
            return new AppPoint(point.X, point.Y);
        }
    }
}