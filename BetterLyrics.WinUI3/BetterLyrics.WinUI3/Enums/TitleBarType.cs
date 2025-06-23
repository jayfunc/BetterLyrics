// 2025/6/23 by Zhe Fang

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Enums
{
    #region Enums

    /// <summary>
    /// Defines the TitleBarType
    /// </summary>
    public enum TitleBarType
    {
        /// <summary>
        /// Defines the Compact
        /// </summary>
        Compact,

        /// <summary>
        /// Defines the Extended
        /// </summary>
        Extended,
    }

    #endregion

    /// <summary>
    /// Defines the <see cref="TitleBarTypeExtensions" />
    /// </summary>
    public static class TitleBarTypeExtensions
    {
        #region Methods

        /// <summary>
        /// The GetHeight
        /// </summary>
        /// <param name="titleBarType">The titleBarType<see cref="TitleBarType"/></param>
        /// <returns>The <see cref="double"/></returns>
        public static double GetHeight(this TitleBarType titleBarType)
        {
            return titleBarType switch
            {
                TitleBarType.Compact => 32.0,
                TitleBarType.Extended => 48.0,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(titleBarType),
                    titleBarType,
                    null
                ),
            };
        }

        #endregion
    }
}
