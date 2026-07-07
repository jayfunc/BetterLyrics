using System;
using BetterLyrics.Core.Enums;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Extensions;

public static class LyricsLayoutOrientationExtensions
{
    extension(LyricsLayoutOrientation orientation)
    {
        public Orientation ToOrientation()
        {
            return orientation switch
            {
                LyricsLayoutOrientation.Horizontal => Orientation.Horizontal,
                LyricsLayoutOrientation.Vertical => Orientation.Vertical,
                _ => throw new ArgumentOutOfRangeException(nameof(orientation))
            };
        }
    }
}