using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace BetterLyrics.WinUI3.Converters
{
    public partial class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is int intVal && intVal != 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
