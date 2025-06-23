// 2025/6/23 by Zhe Fang

using Windows.UI;

namespace BetterLyrics.WinUI3.Helper
{
    /// <summary>
    /// Defines the <see cref="ColorHelper" />
    /// </summary>
    public static class ColorHelper
    {
        #region Methods

        /// <summary>
        /// The GetInterpolatedColor
        /// </summary>
        /// <param name="progress">The progress<see cref="float"/></param>
        /// <param name="startColor">The startColor<see cref="Color"/></param>
        /// <param name="targetColor">The targetColor<see cref="Color"/></param>
        /// <returns>The <see cref="Color"/></returns>
        public static Color GetInterpolatedColor(
            float progress,
            Color startColor,
            Color targetColor
        )
        {
            byte Lerp(byte a, byte b) => (byte)(a + (progress * (b - a)));
            return Color.FromArgb(
                Lerp(startColor.A, targetColor.A),
                Lerp(startColor.R, targetColor.R),
                Lerp(startColor.G, targetColor.G),
                Lerp(startColor.B, targetColor.B)
            );
        }

        /// <summary>
        /// The ToWindowsUIColor
        /// </summary>
        /// <param name="color">The color<see cref="System.Drawing.Color"/></param>
        /// <returns>The <see cref="Windows.UI.Color"/></returns>
        public static Windows.UI.Color ToWindowsUIColor(this System.Drawing.Color color)
        {
            return Windows.UI.Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        #endregion
    }
}
