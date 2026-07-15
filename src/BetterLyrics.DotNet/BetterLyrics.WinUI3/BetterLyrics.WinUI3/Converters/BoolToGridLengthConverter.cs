using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace BetterLyrics.WinUI3.Converters;

public class BoolToGridLengthConverter : IValueConverter
{
    public GridLength TrueValue { get; set; } = new GridLength(1, GridUnitType.Star);
    public GridLength FalseValue { get; set; } = new GridLength(0);
    public bool IsInverted { get; set; }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isTrue)
        {
            if (IsInverted) isTrue = !isTrue;
            return isTrue ? TrueValue : FalseValue;
        }

        return FalseValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
