using BetterLyrics.WinUI3.Helper;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;

namespace BetterLyrics.WinUI3.Converter
{
    public partial class ByteArrayToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is byte[] byteArray && byteArray.Length > 0)
            {
                try
                {
                    using (var ms = new MemoryStream(byteArray))
                    {
                        var stream = ms.AsRandomAccessStream();
                        var bitmapImage = new BitmapImage();

                        bitmapImage.SetSource(stream);

                        return bitmapImage;
                    }
                }
                catch
                {
                    return PathHelper.AlbumArtPlaceholderPath;
                }
            }

            return PathHelper.AlbumArtPlaceholderPath;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
