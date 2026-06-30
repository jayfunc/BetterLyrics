using BetterLyrics.Core.Models.Domain;
using System.Numerics;

namespace BetterLyrics.Core.Extensions
{
    public static class RectExtensions
    {
        extension(AppRect appRect)
        {
            public AppRect WithHeight(double height) => new(
                appRect.X,
                appRect.Y,
                appRect.Width,
                height
            );

            public AppRect WithWidth(double width) => new(
                appRect.X,
                appRect.Y,
                width,
                appRect.Height
            );

            public AppRect WithX(double x) => new(
                x,
                appRect.Y,
                appRect.Width,
                appRect.Height
            );

            public AppRect WithY(double y) => new(
                appRect.X,
                y,
                appRect.Width,
                appRect.Height
            );

            public AppRect AddX(double x) => new(
                appRect.X + x,
                appRect.Y,
                appRect.Width,
                appRect.Height
            );

            public AppRect AddY(double y) => new(
                appRect.X,
                appRect.Y + y,
                appRect.Width,
                appRect.Height
            );

            public AppRect Extend(double left, double top, double right, double bottom) => new(
                appRect.X - left,
                appRect.Y - top,
                appRect.Width + left + right,
                appRect.Height + top + bottom
            );

            public AppRect Extend(double padding) => Extend(appRect, padding, padding, padding, padding);
            public AppRect Extend(double horizontalPadding, double verticalPadding) => Extend(appRect, horizontalPadding, verticalPadding, horizontalPadding, verticalPadding);

            public AppRect Scale(double scale)
            {
                double originalWidth = appRect.Width;
                double originalHeight = appRect.Height;

                double scaledWidth = originalWidth * scale;
                double scaledHeight = originalHeight * scale;

                double scaleOffsetX = (scaledWidth - originalWidth) / 2;
                double scaleOffsetY = (scaledHeight - originalHeight) / 2;

                return new AppRect(
                    appRect.X - scaleOffsetX,
                    appRect.Y - scaleOffsetY,
                    scaledWidth,
                    scaledHeight
                );
            }

            public Vector2 Center => new((float)(appRect.X + appRect.Width / 2), (float)(appRect.Y + appRect.Height / 2));

            public AppRect ToCenterPart(double nX, double nY)
            {
                if (nX <= 0 || nY <= 0) return AppRect.Empty;
                if (appRect.IsEmpty) return AppRect.Empty;

                double targetWidth = appRect.Width / nX;
                double targetHeight = appRect.Height / nY;

                double offsetX = appRect.X + (appRect.Width - targetWidth) / 2.0;
                double offsetY = appRect.Y + (appRect.Height - targetHeight) / 2.0;

                return new AppRect(offsetX, offsetY, targetWidth, targetHeight);
            }

            public AppRect ToCenterPart(double n) => appRect.ToCenterPart(n, n);
        }
    }
}