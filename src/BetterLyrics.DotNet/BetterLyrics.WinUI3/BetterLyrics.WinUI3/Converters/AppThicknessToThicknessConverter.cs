using System;
using BetterLyrics.Core.Models.Domain;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public partial class AppThicknessToThicknessConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is AppThickness appThickness)
            return new Thickness(appThickness.Left, appThickness.Top, appThickness.Right, appThickness.Bottom);
        return new Thickness(0);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}