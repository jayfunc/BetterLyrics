using System;
using System.Collections.Generic;
using System.Linq;
using ATL;
using BetterLyrics.Core.Helpers;
using BetterLyrics.WinUI3.Extensions;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace BetterLyrics.WinUI3.Converters;

public partial class PictureInfosToImageSourceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is IList<PictureInfo> list && list.FirstOrDefault()?.PictureData is byte[] pictureData)
            return BitmapImageExtensions.FromByteArray(pictureData);

        return new BitmapImage(new Uri(PathHelper.AlbumArtPlaceholderPath));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}