using System.Numerics;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Models.Domain;

namespace BetterLyrics.Core.Extensions;

public static class AppColorExtensions
{
    extension(AppColor color)
    {
        public AppColor WithAlpha(byte alpha)
        {
            return new AppColor(alpha, color.R, color.G, color.B);
        }

        public AppColor WithOpacity(float opacity)
        {
            return new AppColor((byte)(opacity * 255), color.R, color.G, color.B);
        }

        /// <summary>
        /// </summary>
        /// <param name="brightness">0-1</param>
        /// <returns></returns>
        public AppColor WithBrightness(double brightness)
        {
            // 确保亮度因子在合理范围内
            brightness = Math.Max(0, Math.Min(1, brightness));

            var (h, s, _) = ColorHelper.ToHsl(color);

            return ColorHelper.FromHsl(h, s, brightness);
        }

        public Vector3 ToVector3RGB()
        {
            return new Vector3((float)color.R / 0xff, (float)color.G / 0xff, (float)color.B / 0xff);
        }
    }
}