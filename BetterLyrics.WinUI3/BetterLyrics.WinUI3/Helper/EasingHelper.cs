// 2025/6/23 by Zhe Fang

using System;

namespace BetterLyrics.WinUI3.Helper
{
    public class EasingHelper
    {
        public static double EaseInOutSine(double t)
        {
            return -(Math.Cos(Math.PI * t) - 1f) / 2f;
        }
        public static double EaseInOutQuad(double t)
        {
            return t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;
        }

        public static double EaseInOutCubic(double t)
        {
            return t < 0.5f ? 4 * t * t * t : 1 - Math.Pow(-2 * t + 2, 3) / 2;
        }
        public static double EaseInOutQuart(double t)
        {
            return t < 0.5f ? 8 * t * t * t * t : 1 - Math.Pow(-2 * t + 2, 4) / 2;
        }

        public static double EaseInOutQuint(double t)
        {
            return t < 0.5f ? 16 * t * t * t * t * t : 1 - Math.Pow(-2 * t + 2, 5) / 2;
        }

        public static double EaseInOutExpo(double t)
        {
            return t == 0
              ? 0
              : t == 1
              ? 1
              : t < 0.5 ? Math.Pow(2, 20 * t - 10) / 2
              : (2 - Math.Pow(2, -20 * t + 10)) / 2;
        }

        public static double EaseInOutCirc(double t)
        {
            return t < 0.5f
              ? (1 - Math.Sqrt(1 - Math.Pow(2 * t, 2))) / 2
              : (Math.Sqrt(1 - Math.Pow(-2 * t + 2, 2)) + 1) / 2;
        }

        public static double EaseInOutBack(double t)
        {
            double c1 = 1.70158f;
            double c2 = c1 * 1.525f;

            return t < 0.5
              ? (Math.Pow(2 * t, 2) * ((c2 + 1) * 2 * t - c2)) / 2
              : (Math.Pow(2 * t - 2, 2) * ((c2 + 1) * (t * 2 - 2) + c2) + 2) / 2;
        }

        public static double EaseInOutElastic(double t)
        {
            if (t == 0 || t == 1) return t;
            double p = 0.3f;
            double s = p / 4;
            return t < 0.5f
              ? -(Math.Pow(2, 20 * t - 10) * Math.Sin((20 * t - 11.125f) * (2 * Math.PI) / p)) / 2
              : (Math.Pow(2, -20 * t + 10) * Math.Sin((20 * t - 11.125f) * (2 * Math.PI) / p)) / 2 + 1;
        }

        private static double EaseOutBounce(double t)
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

        public static double EaseInOutBounce(double t)
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

        public static double SmoothStep(double t)
        {
            return t * t * (3f - 2f * t);
        }

        public static double CubicBezier(double t, double p0, double p1, double p2, double p3)
        {
            double u = 1 - t;
            return u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3;
        }

        public static double Linear(double t) => t;
    }
}
