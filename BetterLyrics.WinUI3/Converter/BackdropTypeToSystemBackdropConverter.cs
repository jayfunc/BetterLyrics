using BetterLyrics.WinUI3.Helper;
using DevWinUI;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Converter {
    public class BackdropTypeToSystemBackdropConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language) {
            if (value is int backdropType) {
                return SystemBackdropHelper.CreateSystemBackdrop((BackdropType)backdropType);
            }
            return SystemBackdropHelper.CreateSystemBackdrop(BackdropType.None);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language) {
            return 0;
        }
    }
}
