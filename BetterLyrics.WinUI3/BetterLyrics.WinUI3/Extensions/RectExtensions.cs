using Windows.Foundation;
using Windows.Graphics;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class RectExtensions
    {
        extension(Rect rect)
        {
            public RectInt32 ToRectInt32() => new(
                (int)rect.X,
                (int)rect.Y,
                (int)rect.Width,
                (int)rect.Height
            );

            public Rect WithHeight(double height) => new(
                rect.X,
                rect.Y,
                rect.Width,
                height
            );

            public Rect WithWidth(double width) => new(
                rect.X,
                rect.Y,
                width,
                rect.Height
            );

            public Rect WithX(double x) => new(
                x,
                rect.Y,
                rect.Width,
                rect.Height
            );

            public Rect WithY(double y) => new(
                rect.X,
                y,
                rect.Width,
                rect.Height
            );
        }
    }
}
