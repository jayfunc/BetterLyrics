using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Text;
using Windows.UI.Text;

namespace BetterLyrics.WinUI3.Enums
{
    public enum LyricsFontWeight
    {
        Thin,
        ExtraLight,
        Light,
        SemiLight,
        Normal,
        Medium,
        SemiBold,
        Bold,
        ExtraBold,
        Black,
        ExtraBlack,
    }

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
                LyricsFontWeight _ => throw new ArgumentOutOfRangeException(
                    nameof(weight),
                    weight,
                    null
                ),
            };
        }
    }
}
