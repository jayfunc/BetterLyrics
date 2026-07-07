using Avalonia;
using BetterLyrics.Core.Models.Domain;

namespace BetterLyrics.Avalonia.Extensions;

public static class PointExtensions
{
    extension(Point point)
    {
        public AppPoint ToAppPoint() => new(point.X, point.Y);
    }
}