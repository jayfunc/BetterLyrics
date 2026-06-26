using BetterLyrics.Core.Enums;
using Microsoft.UI.Xaml.Controls;
using System;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class LyricsLayoutOrientationExtensions
    {
        extension(LyricsLayoutOrientation orientation)
        {
            public Orientation ToOrientation() => orientation switch
            {
                LyricsLayoutOrientation.Horizontal => Orientation.Horizontal,
                LyricsLayoutOrientation.Vertical => Orientation.Vertical,
                _ => throw new ArgumentOutOfRangeException(nameof(orientation)),
            };
        }
    }
}
