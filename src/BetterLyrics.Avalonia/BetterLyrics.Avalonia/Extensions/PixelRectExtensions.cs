using Avalonia;
using BetterLyrics.Core.Models.Domain;

namespace BetterLyrics.Avalonia.Extensions;

public static class PixelRectExtensions
{
    extension(PixelRect pixelRect)
    {
        public AppRect ToAppRect()
        {
            return new AppRect(pixelRect.X, pixelRect.Y, pixelRect.Width, pixelRect.Height);
        }
    }
}