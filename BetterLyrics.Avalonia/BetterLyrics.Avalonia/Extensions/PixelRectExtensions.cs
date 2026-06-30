using Avalonia;
using BetterLyrics.Core.Models.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.Avalonia.Extensions
{
    public static class PixelRectExtensions
    {
        extension(PixelRect pixelRect)
        {
            public AppRect ToAppRect() => new AppRect(pixelRect.X, pixelRect.Y, pixelRect.Width, pixelRect.Height);
        }
    }
}
