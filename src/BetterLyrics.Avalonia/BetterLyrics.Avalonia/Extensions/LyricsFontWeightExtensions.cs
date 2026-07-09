using Avalonia.Media;
using BetterLyrics.Core.Enums;
using System;

namespace BetterLyrics.Avalonia.Extensions;

public static class LyricsFontWeightExtensions
{
    public static FontWeight ToFontWeight(this LyricsFontWeight weight)
    {
        return weight switch
        {
            LyricsFontWeight.Thin => FontWeight.Thin,
            LyricsFontWeight.ExtraLight => FontWeight.ExtraLight,
            LyricsFontWeight.Light => FontWeight.Light,
            LyricsFontWeight.SemiLight => FontWeight.SemiLight,
            LyricsFontWeight.Normal => FontWeight.Normal,
            LyricsFontWeight.Medium => FontWeight.Medium,
            LyricsFontWeight.SemiBold => FontWeight.SemiBold,
            LyricsFontWeight.Bold => FontWeight.Bold,
            LyricsFontWeight.ExtraBold => FontWeight.ExtraBold,
            LyricsFontWeight.Black => FontWeight.Black,
            LyricsFontWeight.ExtraBlack => FontWeight.ExtraBlack,
            _ => throw new ArgumentOutOfRangeException(nameof(weight), weight, null)
        };
    }
}