// 2025/6/23 by Zhe Fang

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    public class EasingHelper
    {
        public static float EaseInOutExpo(float t)
        {
            return t == 0
              ? 0
              : t == 1
              ? 1
              : t < 0.5 ? MathF.Pow(2, 20 * t - 10) / 2
              : (2 - MathF.Pow(2, -20 * t + 10)) / 2;
        }

        public static float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;
        }

        public static float EaseInQuad(float t) => t * t;

        public static float EaseOutQuad(float t) => t * (2 - t);

        public static float Linear(float t) => t;

        public static float SmootherStep(float t)
        {
            return t * t * t * (t * (6 * t - 15) + 10);
        }

        public static float SmoothStep(float t)
        {
            return t * t * (3 - 2 * t);
        }
    }
}
