using Avalonia;
using BetterLyrics.Core.Models.Domain;

namespace BetterLyrics.Avalonia.Extensions
{
    public static class RectExtensions
    {
        public static Rect FromAppRect(AppRect appRect)
        {
            return new Rect(appRect.X, appRect.Y, appRect.Width, appRect.Height);
        }

        public static AppRect ToAppRect(this Rect rect)
        {
            return new AppRect(rect.X, rect.Y, rect.Width, rect.Height);
        }

        public static Rect Scale(this Rect rect, double scale)
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

        public static Rect Extend(this Rect rect, double left, double top, double right, double bottom)
        {
            return new Rect(rect.X - left, rect.Y - top, rect.Width + left + right, rect.Height + top + bottom);
        }

        public static Rect Extend(this Rect rect, double padding)
        {
            return rect.Extend(padding, padding, padding, padding);
        }

        public static Rect Extend(this Rect rect, double horizontalPadding, double verticalPadding)
        {
            return rect.Extend(horizontalPadding, verticalPadding, horizontalPadding, verticalPadding);
        }
    }
}