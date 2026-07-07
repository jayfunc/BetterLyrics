using BetterLyrics.Core.Helpers;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;

namespace BetterLyrics.WinUI3.Extensions
{
    public static class BitmapImageExtensions
    {
        public static BitmapImage FromByteArray(byte[] bytes)
        {
            if (bytes != null)
                try
                {
                    using (var ms = new MemoryStream(bytes))
                    {
                        var stream = ms.AsRandomAccessStream();

                        var bitmapImage = new BitmapImage();
                        bitmapImage.SetSource(stream);
                        return bitmapImage;
                    }
                }
                catch
                {
                }

            return new BitmapImage(new Uri(PathHelper.AlbumArtPlaceholderPath));
        }
    }
}
