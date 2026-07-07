using System;
using Windows.UI.Text;
using BetterLyrics.Core.Enums;
using Microsoft.UI.Text;

namespace BetterLyrics.WinUI3.Extensions;

public static class LyricsFontWeightExtensions
{
    public static FontWeight ToFontWeight(this LyricsFontWeight weight)
    {
        return weight switch
        {
            LyricsFontWeight.Thin => FontWeights.Thin,
            LyricsFontWeight.ExtraLight => FontWeights.ExtraLight,
            LyricsFontWeight.Light => FontWeights.Light,
            LyricsFontWeight.SemiLight => FontWeights.SemiLight,
            LyricsFontWeight.Normal => FontWeights.Normal,
            LyricsFontWeight.Medium => FontWeights.Medium,
            LyricsFontWeight.SemiBold => FontWeights.SemiBold,
            LyricsFontWeight.Bold => FontWeights.Bold,
            LyricsFontWeight.ExtraBold => FontWeights.ExtraBold,
            LyricsFontWeight.Black => FontWeights.Black,
            LyricsFontWeight.ExtraBlack => FontWeights.ExtraBlack,
            _ => throw new ArgumentOutOfRangeException(nameof(weight), weight, null)
        };
    }
}