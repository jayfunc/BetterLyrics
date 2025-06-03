using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper {
    public class EasingHelper {

        /// <summary>
        /// No easing
        /// </summary>
        public static float Linear(float t) => t;

        /// <summary>
        /// Accelerating from 0
        /// </summary>
        public static float EaseInQuad(float t) => t * t;

        /// <summary>
        /// Decelerating to 0
        /// </summary>
        public static float EaseOutQuad(float t) => t * (2 - t);

        /// <summary>
        /// Acceleration until halfway then deceleration
        /// </summary>
        public static float EaseInOutQuad(float t) {
            return t < 0.5f
                ? 2 * t * t
                : -1 + (4 - 2 * t) * t;
        }

        /// <summary>
        /// Smoother transition than linear
        /// </summary>
        public static float SmoothStep(float t) {
            return t * t * (3 - 2 * t);
        }

        /// <summary>
        /// Even smoother transition with continuous first and second derivatives
        /// </summary>
        public static float SmootherStep(float t) {
            return t * t * t * (t * (6 * t - 15) + 10);
        }
    }
}
