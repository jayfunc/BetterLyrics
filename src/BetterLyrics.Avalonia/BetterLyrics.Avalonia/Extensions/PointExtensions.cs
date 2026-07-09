using Avalonia;
using BetterLyrics.Core.Models.Domain;

namespace BetterLyrics.Avalonia.Extensions;

public static class PointExtensions
{
    extension(Point point)
    {
        public AppPoint ToAppPoint() => new(point.X, point.Y);

        public Point AddX(double value) => new(point.X + value, point.Y);
        public Point AddY(double value) => new(point.X, point.Y + value);
    }
}