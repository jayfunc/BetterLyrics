using System.Drawing;
using System.Numerics;
using Windows.Foundation;
using Windows.Graphics;
using BetterLyrics.Core.Models.Domain;
using Vanara.PInvoke;

namespace BetterLyrics.WinUI3.Extensions;

public static class RectExtensions
{
    extension(Rect rect)
    {
        public Vector2 Center => new((float)(rect.X + rect.Width / 2), (float)(rect.Y + rect.Height / 2));

        public RectInt32 ToRectInt32()
        {
            return new RectInt32(
                (int)rect.X,
                (int)rect.Y,
                (int)rect.Width,
                (int)rect.Height
            );
        }

        public Rectangle ToRectangle()
        {
            return new Rectangle(
                (int)rect.X,
                (int)rect.Y,
                (int)rect.Width,
                (int)rect.Height
            );
        }

        public Rect WithHeight(double height)
        {
            return new Rect(
                rect.X,
                rect.Y,
                rect.Width,
                height
            );
        }

        public Rect WithWidth(double width)
        {
            return new Rect(
                rect.X,
                rect.Y,
                width,
                rect.Height
            );
        }

        public Rect WithX(double x)
        {
            return new Rect(
                x,
                rect.Y,
                rect.Width,
                rect.Height
            );
        }

        public Rect WithY(double y)
        {
            return new Rect(
                rect.X,
                y,
                rect.Width,
                rect.Height
            );
        }

        public Rect AddX(double x)
        {
            return new Rect(
                rect.X + x,
                rect.Y,
                rect.Width,
                rect.Height
            );
        }

        public Rect AddY(double y)
        {
            return new Rect(
                rect.X,
                rect.Y + y,
                rect.Width,
                rect.Height
            );
        }

        public Rect Extend(double left, double top, double right, double bottom)
        {
            return new Rect(
                rect.X - left,
                rect.Y - top,
                rect.Width + left + right,
                rect.Height + top + bottom
            );
        }

        public Rect Extend(double padding)
        {
            return rect.Extend(padding, padding, padding, padding);
        }

        public Rect Extend(double horizontalPadding, double verticalPadding)
        {
            return rect.Extend(horizontalPadding, verticalPadding, horizontalPadding, verticalPadding);
        }

        public Rect Scale(double scale)
        {
            var originalWidth = rect.Width;
            var originalHeight = rect.Height;

            var scaledWidth = originalWidth * scale;
            var scaledHeight = originalHeight * scale;

            var scaleOffsetX = (scaledWidth - originalWidth) / 2;
            var scaleOffsetY = (scaledHeight - originalHeight) / 2;

            return new Rect(
                rect.X - scaleOffsetX,
                rect.Y - scaleOffsetY,
                scaledWidth,
                scaledHeight
            );
        }

        public Rect ToCenterPart(double nX, double nY)
        {
            if (nX <= 0 || nY <= 0) return Rect.Empty;
            if (rect.IsEmpty) return Rect.Empty;

            var targetWidth = rect.Width / nX;
            var targetHeight = rect.Height / nY;

            var offsetX = rect.X + (rect.Width - targetWidth) / 2.0;
            var offsetY = rect.Y + (rect.Height - targetHeight) / 2.0;

            return new Rect(offsetX, offsetY, targetWidth, targetHeight);
        }

        public Rect ToCenterPart(double n)
        {
            return rect.ToCenterPart(n, n);
        }

        public AppRect ToAppRect()
        {
            return new AppRect(
                rect.Left,
                rect.Top,
                rect.Width,
                rect.Height
            );
        }
    }

    extension(RECT rect)
    {
        public Rect ToRect()
        {
            return new Rect(
                rect.Left,
                rect.Top,
                rect.Width,
                rect.Height
            );
        }

        public AppRect ToAppRect()
        {
            return new AppRect(
                rect.Left,
                rect.Top,
                rect.Width,
                rect.Height
            );
        }
    }
}