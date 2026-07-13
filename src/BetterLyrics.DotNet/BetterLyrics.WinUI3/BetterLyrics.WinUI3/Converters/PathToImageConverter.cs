using System;
using System.IO;
using BetterLyrics.Core.Helpers;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace BetterLyrics.WinUI3.Converters;

public partial class PathToImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var targetPath = PathHelper.AlbumArtPlaceholderPath;
        if (value is string path)
            if (File.Exists(path))
                targetPath = path;

        return new BitmapImage(new Uri(targetPath));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}