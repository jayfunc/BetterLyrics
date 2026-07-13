using System;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3.Extensions;

public static class GridLengthExtensions
{
    public static GridLength ParseGridLength(string str, double scale = 1.0)
    {
        if (string.IsNullOrWhiteSpace(str)) return new GridLength(1, GridUnitType.Star);
        if (str.Equals("Auto", StringComparison.OrdinalIgnoreCase)) return new GridLength(1, GridUnitType.Auto);

        if (str.EndsWith("*"))
        {
            var starStr = str.TrimEnd('*');
            if (string.IsNullOrEmpty(starStr)) return new GridLength(1, GridUnitType.Star);
            if (double.TryParse(starStr, out var starValue))
                return new GridLength(starValue, GridUnitType.Star);
        }

        var pxValue = str.EndsWith("px", StringComparison.OrdinalIgnoreCase) ? str.Substring(0, str.Length - 2) : str;
        if (double.TryParse(pxValue, out var absoluteValue))
            return new GridLength(absoluteValue * scale, GridUnitType.Pixel);

        return new GridLength(1, GridUnitType.Star);
    }
}