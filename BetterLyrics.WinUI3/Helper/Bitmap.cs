using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace BetterLyrics.WinUI3.Helper
{
    public class Bitmap
    {
        public static async Task<BitmapImage> LoadBitmapImageFromBytesAsync(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return null;

            // 创建 BitmapImage
            var bitmapImage = new BitmapImage();

            // 创建内存流
            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                // 写入 byte[] 到流
                await stream.WriteAsync(imageBytes.AsBuffer());
                stream.Seek(0); // 重置指针到起始位置

                // 设置 BitmapImage 的源
                await bitmapImage.SetSourceAsync(stream);
            }

            return bitmapImage;
        }
    }
}
