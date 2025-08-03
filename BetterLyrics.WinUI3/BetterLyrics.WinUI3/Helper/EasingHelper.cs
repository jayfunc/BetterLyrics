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
        public static float EaseInOutSine(float t)
        {
            return -(MathF.Cos(MathF.PI * t) - 1f) / 2f;
        }
        public static float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;
        }

        public static float EaseInOutCubic(float t)
        {
            return t < 0.5f ? 4 * t * t * t : 1 - MathF.Pow(-2 * t + 2, 3) / 2;
        }
        public static float EaseInOutQuart(float t)
        {
            return t < 0.5f ? 8 * t * t * t * t : 1 - MathF.Pow(-2 * t + 2, 4) / 2;
        }

        public static float EaseInOutQuint(float t)
        {
            return t < 0.5f ? 16 * t * t * t * t * t : 1 - MathF.Pow(-2 * t + 2, 5) / 2;
        }

        public static float EaseInOutExpo(float t)
        {
            return t == 0
              ? 0
              : t == 1
              ? 1
              : t < 0.5 ? MathF.Pow(2, 20 * t - 10) / 2
              : (2 - MathF.Pow(2, -20 * t + 10)) / 2;
        }

        public static float EaseInOutCirc(float t)
        {
            return t < 0.5f
              ? (1 - MathF.Sqrt(1 - MathF.Pow(2 * t, 2))) / 2
              : (MathF.Sqrt(1 - MathF.Pow(-2 * t + 2, 2)) + 1) / 2;
        }

        public static float EaseInOutBack(float t)
        {
            float c1 = 1.70158f;
            float c2 = c1 * 1.525f;

            return t < 0.5
              ? (MathF.Pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2)) / 2
              : (MathF.Pow(2 * t - 2, 2) * ((c2 + 1) * (t * 2 - 2) + c2) + 2) / 2;
        }

        public static float EaseInOutElastic(float t)
        {
            if (t == 0 || t == 1) return t;
            float p = 0.3f;
            float s = p / 4;
            return t < 0.5f
              ? -(MathF.Pow(2, 20 * t - 10) * MathF.Sin((20 * t - 11.125f) * (2 * MathF.PI) / p)) / 2
              : (MathF.Pow(2, -20 * t + 10) * MathF.Sin((20 * t - 11.125f) * (2 * MathF.PI) / p)) / 2 + 1;
        }

        private static float EaseOutBounce(float t)
        {
            if (t < 4 / 11f)
            {
                return (121 * t * t) / 16f;
            }
            else if (t < 8 / 11f)
            {
                return (363 / 40f * t * t) - (99 / 10f * t) + 17 / 5f;
            }
            else if (t < 9 / 10f)
            {
                return (4356 / 361f * t * t) - (35442 / 1805f * t) + 16061 / 1805f;
            }
            else
            {
                return (54 / 5f * t * t) - (513 / 25f * t) + 268 / 25f;
            }
        }

        public static float EaseInOutBounce(float t)
        {
            if (t < 0.5f)
            {
                return (1 - EaseOutBounce(1 - 2 * t)) / 2;
            }
            else
            {
                return (1 + EaseOutBounce(2 * t - 1)) / 2;
            }
        }

        public static float SmoothStep(float t)
        {
            return t * t * (3f - 2f * t);
        }

        public static float CubicBezier(float t, float p0, float p1, float p2, float p3)
        {
            float u = 1 - t;
            return u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3;
        }

        public static float Linear(float t) => t;
    }
}
