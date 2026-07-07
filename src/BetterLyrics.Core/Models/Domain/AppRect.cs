using System.Numerics;
using System.Text.Json.Serialization;

namespace BetterLyrics.Core.Models.Domain;

public record AppRect(double X, double Y, double Width, double Height)
{
    public static readonly AppRect Empty = new(0, 0, 0, 0);
    [JsonIgnore] public double Left => X;
    [JsonIgnore] public double Top => Y;
    [JsonIgnore] public double Right => X + Width;
    [JsonIgnore] public double Bottom => Y + Height;
    [JsonIgnore] public bool IsEmpty => Width <= 0 || Height <= 0;

    public bool IntersectsWith(AppRect rect)
    {
        return !(rect.Left >= Right ||
                 rect.Right <= Left ||
                 rect.Top >= Bottom ||
                 rect.Bottom <= Top);
    }

    public AppRect Translate(double offsetX, double offsetY)
    {
        return new AppRect(X + offsetX, Y + offsetY, Width, Height);
    }

    public AppRect Translate(Vector2 vector2)
    {
        return Translate(vector2.X, vector2.Y);
    }
}