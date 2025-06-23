// 2025/6/23 by Zhe Fang

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    /// <summary>
    /// Defines the <see cref="EasingHelper" />
    /// </summary>
    public class EasingHelper
    {
        #region Methods

        /// <summary>
        /// Acceleration until halfway then deceleration
        /// </summary>
        /// <param name="t">The t<see cref="float"/></param>
        /// <returns>The <see cref="float"/></returns>
        public static float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;
        }

        /// <summary>
        /// Accelerating from 0
        /// </summary>
        /// <param name="t">The t<see cref="float"/></param>
        /// <returns>The <see cref="float"/></returns>
        public static float EaseInQuad(float t) => t * t;

        /// <summary>
        /// Decelerating to 0
        /// </summary>
        /// <param name="t">The t<see cref="float"/></param>
        /// <returns>The <see cref="float"/></returns>
        public static float EaseOutQuad(float t) => t * (2 - t);

        /// <summary>
        /// No easing
        /// </summary>
        /// <param name="t">The t<see cref="float"/></param>
        /// <returns>The <see cref="float"/></returns>
        public static float Linear(float t) => t;

        /// <summary>
        /// Even smoother transition with continuous first and second derivatives
        /// </summary>
        /// <param name="t">The t<see cref="float"/></param>
        /// <returns>The <see cref="float"/></returns>
        public static float SmootherStep(float t)
        {
            return t * t * t * (t * (6 * t - 15) + 10);
        }

        /// <summary>
        /// Smoother transition than linear
        /// </summary>
        /// <param name="t">The t<see cref="float"/></param>
        /// <returns>The <see cref="float"/></returns>
        public static float SmoothStep(float t)
        {
            return t * t * (3 - 2 * t);
        }

        #endregion
    }
}
