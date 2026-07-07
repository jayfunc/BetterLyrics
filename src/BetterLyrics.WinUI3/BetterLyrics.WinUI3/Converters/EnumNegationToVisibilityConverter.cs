using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public partial class EnumNegationToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null || parameter == null) return Visibility.Collapsed;

        if (!IsMatch(value, parameter)) return Visibility.Visible;

        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    private bool IsMatch(object value, object parameter)
    {
        string? valueString;

        if (value.GetType().IsEnum)
            valueString = ((int)value).ToString();
        else
            valueString = value.ToString();

        return valueString == parameter.ToString();
    }
}