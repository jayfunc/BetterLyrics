using System.Numerics;
using BetterLyrics.Core.Models.Domain;

namespace BetterLyrics.Core.Extensions;

public static class AppRectExtensions
{
    extension(AppRect appRect)
    {
        public Vector2 Center => new((float)(appRect.X + appRect.Width / 2), (float)(appRect.Y + appRect.Height / 2));

        public AppRect WithHeight(double height)
        {
            return new AppRect(
                appRect.X,
                appRect.Y,
                appRect.Width,
                height
            );
        }

        public AppRect WithWidth(double width)
        {
            return new AppRect(
                appRect.X,
                appRect.Y,
                width,
                appRect.Height
            );
        }

        public AppRect WithX(double x)
        {
            return new AppRect(
                x,
                appRect.Y,
                appRect.Width,
                appRect.Height
            );
        }

        public AppRect WithY(double y)
        {
            return new AppRect(
                appRect.X,
                y,
                appRect.Width,
                appRect.Height
            );
        }

        public AppRect AddX(double x)
        {
            return new AppRect(
                appRect.X + x,
                appRect.Y,
                appRect.Width,
                appRect.Height
            );
        }

        public AppRect AddY(double y)
        {
            return new AppRect(
                appRect.X,
                appRect.Y + y,
                appRect.Width,
                appRect.Height
            );
        }

        public AppRect Extend(double left, double top, double right, double bottom)
        {
            return new AppRect(
                appRect.X - left,
                appRect.Y - top,
                appRect.Width + left + right,
                appRect.Height + top + bottom
            );
        }

        public AppRect Extend(double padding)
        {
            return appRect.Extend(padding, padding, padding, padding);
        }

        public AppRect Extend(double horizontalPadding, double verticalPadding)
        {
            return appRect.Extend(horizontalPadding, verticalPadding, horizontalPadding, verticalPadding);
        }

        public AppRect Scale(double scale)
        {
            var originalWidth = appRect.Width;
            var originalHeight = appRect.Height;

            var scaledWidth = originalWidth * scale;
            var scaledHeight = originalHeight * scale;

            var scaleOffsetX = (scaledWidth - originalWidth) / 2;
            var scaleOffsetY = (scaledHeight - originalHeight) / 2;

            return new AppRect(
                appRect.X - scaleOffsetX,
                appRect.Y - scaleOffsetY,
                scaledWidth,
                scaledHeight
            );
        }

        public AppRect ToCenterPart(double nX, double nY)
        {
            if (nX <= 0 || nY <= 0) return AppRect.Empty;
            if (appRect.IsEmpty) return AppRect.Empty;

            var targetWidth = appRect.Width / nX;
            var targetHeight = appRect.Height / nY;

            var offsetX = appRect.X + (appRect.Width - targetWidth) / 2.0;
            var offsetY = appRect.Y + (appRect.Height - targetHeight) / 2.0;

            return new AppRect(offsetX, offsetY, targetWidth, targetHeight);
        }

        public AppRect ToCenterPart(double n)
        {
            return appRect.ToCenterPart(n, n);
        }
    }
}