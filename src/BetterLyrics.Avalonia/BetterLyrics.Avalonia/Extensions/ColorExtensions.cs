using Avalonia.Media;
using BetterLyrics.Core.Models.Domain;

namespace BetterLyrics.Avalonia.Extensions;

public static class ColorExtensions
{
    public static Color FromAppColor(AppColor appColor)
    {
        return new Color(appColor.A, appColor.R, appColor.G, appColor.B);
    }

    public static AppColor ToAppColor(this Color color)
    {
        return new AppColor(color.A, color.R, color.G, color.B);
    }
}
