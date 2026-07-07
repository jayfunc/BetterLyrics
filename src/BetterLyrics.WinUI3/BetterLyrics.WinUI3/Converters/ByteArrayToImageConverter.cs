using BetterLyrics.Core.Helpers;
using BetterLyrics.WinUI3.Extensions;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace BetterLyrics.WinUI3.Converters;

public partial class ByteArrayToImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is byte[] byteArray) return BitmapImageExtensions.FromByteArray(byteArray);

        return new BitmapImage(new Uri(PathHelper.AlbumArtPlaceholderPath));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}