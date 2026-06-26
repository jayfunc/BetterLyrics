using System.Text.Json.Serialization;

namespace BetterLyrics.Core.Models.Domain
{
    public record AppRect(double X, double Y, double Width, double Height)
    {
        [JsonIgnore] public double Left => X;
        [JsonIgnore] public double Top => Y;
        [JsonIgnore] public double Right => X + Width;
        [JsonIgnore] public double Bottom => Y + Height;
        [JsonIgnore] public bool IsEmpty => Width <= 0 || Height <= 0;

        public static readonly AppRect Empty = new(0, 0, 0, 0);

        public bool IntersectsWith(AppRect rect)
        {
            return !(rect.Left >= this.Right ||
                     rect.Right <= this.Left ||
                     rect.Top >= this.Bottom ||
                     rect.Bottom <= this.Top);
        }
    }
}