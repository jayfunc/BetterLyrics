using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.ViewModels;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converter
{
    public class IntToCornerRadius : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int intValue && parameter is double controlHeight)
            {
                return new Microsoft.UI.Xaml.CornerRadius(intValue / 100f * controlHeight / 2);
            }
            return new Microsoft.UI.Xaml.CornerRadius(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
