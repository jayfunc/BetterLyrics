using System;
using Windows.UI;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.WinUI3.Extensions;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public partial class AppColorToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is AppColor appColor) return ColorExtensions.FromAppColor(appColor);
        return Colors.Transparent;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Color color) return color.ToAppColor();
        return Core.Constants.Colors.Transparent;
    }
}