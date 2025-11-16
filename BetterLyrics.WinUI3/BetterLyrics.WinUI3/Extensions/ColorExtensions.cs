using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Windows.UI;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class ColorExtensions
    {
        extension(Color color)
        {
            public Color WithAlpha(byte alpha)
            {
                return Color.FromArgb(alpha, color.R, color.G, color.B);
            }

            public Color WithOpacity(float opacity)
            {
                return Color.FromArgb((byte)(opacity * 255), color.R, color.G, color.B);
            }

            public Color WithBrightness(double brightness)
            {
                // 确保亮度因子在合理范围内
                brightness = Math.Max(0, Math.Min(1, brightness));

                var hsl = CommunityToolkit.WinUI.Helpers.ColorHelper.ToHsl(color);
                double h = hsl.H;
                double s = hsl.S;

                return CommunityToolkit.WinUI.Helpers.ColorHelper.FromHsl(h, s, brightness);
            }

            public Vector3 ToVector3RGB()
            {
                return new Vector3((float)color.R / 0xff, (float)color.G / 0xff, (float)color.B / 0xff);
            }

        }
    }
}
