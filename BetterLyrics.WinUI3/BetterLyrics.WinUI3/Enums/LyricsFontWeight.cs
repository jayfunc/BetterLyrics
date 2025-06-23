// 2025/6/23 by Zhe Fang

using Microsoft.UI.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Text;

namespace BetterLyrics.WinUI3.Enums
{
    #region Enums

    /// <summary>
    /// Defines the LyricsFontWeight
    /// </summary>
    public enum LyricsFontWeight
    {
        /// <summary>
        /// Defines the Thin
        /// </summary>
        Thin,

        /// <summary>
        /// Defines the ExtraLight
        /// </summary>
        ExtraLight,

        /// <summary>
        /// Defines the Light
        /// </summary>
        Light,

        /// <summary>
        /// Defines the SemiLight
        /// </summary>
        SemiLight,

        /// <summary>
        /// Defines the Normal
        /// </summary>
        Normal,

        /// <summary>
        /// Defines the Medium
        /// </summary>
        Medium,

        /// <summary>
        /// Defines the SemiBold
        /// </summary>
        SemiBold,

        /// <summary>
        /// Defines the Bold
        /// </summary>
        Bold,

        /// <summary>
        /// Defines the ExtraBold
        /// </summary>
        ExtraBold,

        /// <summary>
        /// Defines the Black
        /// </summary>
        Black,

        /// <summary>
        /// Defines the ExtraBlack
        /// </summary>
        ExtraBlack,
    }

    #endregion

    /// <summary>
    /// Defines the <see cref="LyricsFontWeightExtensions" />
    /// </summary>
    public static class LyricsFontWeightExtensions
    {
        #region Methods

        /// <summary>
        /// The ToFontWeight
        /// </summary>
        /// <param name="weight">The weight<see cref="LyricsFontWeight"/></param>
        /// <returns>The <see cref="FontWeight"/></returns>
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

        #endregion
    }
}
