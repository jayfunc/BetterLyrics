using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace BetterLyrics.WinUI3.Helper
{
    public class ImageHelper
    {
        public static async Task<InMemoryRandomAccessStream> GetStreamFromBytesAsync(
            byte[] imageBytes
        )
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return null;

            InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(imageBytes.AsBuffer());
            stream.Seek(0);

            return stream;
        }

        public static async Task<byte[]> ToByteArrayAsync(IRandomAccessStreamReference streamRef)
        {
            using IRandomAccessStream stream = await streamRef.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.AsStreamForRead().CopyToAsync(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
