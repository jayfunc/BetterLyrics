using BetterLyrics.Core.Helpers;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;

namespace BetterLyrics.WinUI3.Converters;

public partial class PathToImageConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var targetPath = PathHelper.AlbumArtPlaceholderPath;

        if (Uri.TryCreate(value as string, UriKind.Absolute, out var outUri))
        {
            if (outUri.Scheme == Uri.UriSchemeFile)
            {
                if (File.Exists(outUri.LocalPath))
                {
                    targetPath = outUri.AbsoluteUri;
                }
            }
            else if (outUri.Scheme == "ms-appx")
            {
                targetPath = outUri.AbsoluteUri;
            }
        }

        return new BitmapImage(new Uri(targetPath));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}