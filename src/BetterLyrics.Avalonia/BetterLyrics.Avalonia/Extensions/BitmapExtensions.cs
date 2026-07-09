using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BetterLyrics.Avalonia.Extensions
{
    public static class BitmapExtensions
    {
        public static Bitmap? FromByteArray(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
                return null;

            using (var memoryStream = new MemoryStream(imageData))
            {
                memoryStream.Position = 0;

                return new Bitmap(memoryStream);
            }
        }
    }
}
