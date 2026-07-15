using System;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converters;

public sealed class DateTimeToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is DateTime dt)
        {
            if (dt == DateTime.MinValue) return string.Empty;
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }
        if (value is DateTimeOffset dto)
        {
            if (dto == DateTimeOffset.MinValue) return string.Empty;
            return dto.ToString("yyyy-MM-dd HH:mm:ss");
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
