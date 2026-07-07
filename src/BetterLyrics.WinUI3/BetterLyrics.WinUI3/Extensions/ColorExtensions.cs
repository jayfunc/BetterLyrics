using System.Numerics;
using Windows.UI;
using BetterLyrics.Core.Models.Domain;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace BetterLyrics.WinUI3.Extensions;

public static class ColorExtensions
{
    public static Color FromAppColor(AppColor appColor)
    {
        return Color.FromArgb(appColor.A, appColor.R, appColor.G, appColor.B);
    }

    extension(Color color)
    {
        public Color WithAlpha(byte alpha)
        {
            return Color.FromArgb(alpha, color.R, color.G, color.B);
        }

        public Vector3 ToVector3RGB()
        {
            return new Vector3((float)color.R / 0xff, (float)color.G / 0xff, (float)color.B / 0xff);
        }

        public SolidColorPaint ToPaint()
        {
            var skColor = new SKColor(color.R, color.G, color.B, color.A);
            return new SolidColorPaint(skColor);
        }

        public AppColor ToAppColor()
        {
            return new AppColor(color.A, color.R, color.G, color.B);
        }
    }
}