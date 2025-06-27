using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Data;

namespace BetterLyrics.WinUI3.Converter
{
    internal partial class CornerRadiusToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is Microsoft.UI.Xaml.CornerRadius cornerRadius)
            {
                // Convert CornerRadius to an integer value, e.g., using the top-left radius
                return (double)cornerRadius.TopLeft;
            }
            return .0; // or handle the case where value is not a CornerRadius
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
