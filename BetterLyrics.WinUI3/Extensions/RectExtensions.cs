using BetterLyrics.Core.Models.Domain;
using System.Drawing;
using System.Numerics;
using Vanara.PInvoke;
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

            public Rectangle ToRectangle() => new(
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

            public Rect AddX(double x) => new(
                rect.X + x,
                rect.Y,
                rect.Width,
                rect.Height
            );

            public Rect AddY(double y) => new(
                rect.X,
                rect.Y + y,
                rect.Width,
                rect.Height
            );

            public Rect Extend(double left, double top, double right, double bottom) => new(
                rect.X - left,
                rect.Y - top,
                rect.Width + left + right,
                rect.Height + top + bottom
            );

            public Rect Extend(double padding) => Extend(rect, padding, padding, padding, padding);
            public Rect Extend(double horizontalPadding, double verticalPadding) => Extend(rect, horizontalPadding, verticalPadding, horizontalPadding, verticalPadding);

            public Rect Scale(double scale)
            {
                double originalWidth = rect.Width;
                double originalHeight = rect.Height;

                double scaledWidth = originalWidth * scale;
                double scaledHeight = originalHeight * scale;

                double scaleOffsetX = (scaledWidth - originalWidth) / 2;
                double scaleOffsetY = (scaledHeight - originalHeight) / 2;

                return new Rect(
                    rect.X - scaleOffsetX,
                    rect.Y - scaleOffsetY,
                    scaledWidth,
                    scaledHeight
                );
            }

            public Vector2 Center => new((float)(rect.X + rect.Width / 2), (float)(rect.Y + rect.Height / 2));

            public Rect ToCenterPart(double nX, double nY)
            {
                if (nX <= 0 || nY <= 0) return Rect.Empty;
                if (rect.IsEmpty) return Rect.Empty;

                double targetWidth = rect.Width / nX;
                double targetHeight = rect.Height / nY;

                double offsetX = rect.X + (rect.Width - targetWidth) / 2.0;
                double offsetY = rect.Y + (rect.Height - targetHeight) / 2.0;

                return new Rect(offsetX, offsetY, targetWidth, targetHeight);
            }

            public Rect ToCenterPart(double n) => rect.ToCenterPart(n, n);

            public AppRect ToAppRect() => new(
                rect.Left,
                rect.Top,
                rect.Width,
                rect.Height
            );
        }

        extension(RECT rect)
        {
            public Rect ToRect() => new(
                rect.Left,
                rect.Top,
                rect.Width,
                rect.Height
            );

            public AppRect ToAppRect() => new(
                rect.Left,
                rect.Top,
                rect.Width,
                rect.Height
            );
        }
    }
}
