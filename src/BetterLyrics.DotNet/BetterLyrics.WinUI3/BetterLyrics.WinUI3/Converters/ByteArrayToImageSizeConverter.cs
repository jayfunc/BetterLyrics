using BetterLyrics.WinUI3.Extensions;
using Microsoft.UI.Xaml.Data;
using System;

namespace BetterLyrics.WinUI3.Converters;

public partial class ByteArrayToImageSizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is byte[] byteArray)
        {
            var bitmapImage = BitmapImageExtensions.FromByteArray(byteArray);
            string param = parameter?.ToString() ?? "";
            
            if (param.Equals("Width", StringComparison.OrdinalIgnoreCase))
                return bitmapImage.PixelWidth.ToString();
                
            if (param.Equals("Height", StringComparison.OrdinalIgnoreCase))
                return bitmapImage.PixelHeight.ToString();
        }

        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
