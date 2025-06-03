using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Converter {
    internal class ThemeTypeToElementThemeConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value is int themeType) {
                return (ElementTheme)themeType;
            }
            return ElementTheme.Default;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return 0;
        }
    }
}
