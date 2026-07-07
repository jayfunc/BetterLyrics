using System;
using BetterLyrics.Core.Enums;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public partial class MessageSeverityToInfoBarSeverity : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is MessageSeverity severity)
            return severity switch
            {
                MessageSeverity.Informational => InfoBarSeverity.Informational,
                MessageSeverity.Success => InfoBarSeverity.Success,
                MessageSeverity.Warning => InfoBarSeverity.Warning,
                MessageSeverity.Error => InfoBarSeverity.Error,
                _ => new ArgumentOutOfRangeException(nameof(severity))
            };
        return InfoBarSeverity.Informational;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}