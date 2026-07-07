using Avalonia.Media;
using BetterLyrics.Core.Models.Domain;

namespace BetterLyrics.Avalonia.Extensions;

public static class ColorExtensions
{
    public static Color FromAppColor(AppColor appColor)
    {
        return new Color(appColor.A, appColor.R, appColor.G, appColor.B);
    }
}
