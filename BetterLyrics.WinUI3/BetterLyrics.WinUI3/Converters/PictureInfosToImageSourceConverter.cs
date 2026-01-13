using ATL;
using BetterLyrics.WinUI3.Helper;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BetterLyrics.WinUI3.Converters
{
    public partial class PictureInfosToImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            BitmapImage bitmapImage = new();
            if (value is IList<PictureInfo> list && list.FirstOrDefault()?.PictureData is byte[] pictureData)
            {
                bitmapImage.SetSource(ImageHelper.ToIRandomAccessStream(pictureData));
            }
            else
            {
                bitmapImage.UriSource = new Uri(PathHelper.AlbumArtPlaceholderPath);
            }
            return bitmapImage;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
