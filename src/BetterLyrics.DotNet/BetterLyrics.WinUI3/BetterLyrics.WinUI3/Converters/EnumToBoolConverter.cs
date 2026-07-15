using System;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public partial class EnumToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value == null || parameter == null) return false;

        if (IsMatch(value, parameter)) return true;

        return false;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

    private bool IsMatch(object value, object parameter)
    {
        string? valueString;

        if (value.GetType().IsEnum)
            valueString = System.Convert.ToInt32(value).ToString();
        else
            valueString = value.ToString();

        return valueString == parameter.ToString();
    }
}