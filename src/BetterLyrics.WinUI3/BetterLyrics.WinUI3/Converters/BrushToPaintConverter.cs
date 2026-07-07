using System;
using BetterLyrics.WinUI3.Extensions;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace BetterLyrics.WinUI3.Converters;

public partial class BrushToPaintConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is SolidColorBrush solidColorBrush) return solidColorBrush.Color.ToPaint();
        return Colors.Transparent.ToPaint();
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}