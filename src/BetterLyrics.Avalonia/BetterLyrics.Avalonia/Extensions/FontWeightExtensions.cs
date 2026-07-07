using System;
using Avalonia.Media;
using BetterLyrics.Core.Enums;

namespace BetterLyrics.Avalonia.Extensions;

public static class FontWeightExtensions
{
    public static FontWeight FromLyricsFontWeight(LyricsFontWeight lyricsFontWeight) => lyricsFontWeight switch
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
        _ => throw new ArgumentOutOfRangeException(nameof(lyricsFontWeight), lyricsFontWeight, null)
    };
}