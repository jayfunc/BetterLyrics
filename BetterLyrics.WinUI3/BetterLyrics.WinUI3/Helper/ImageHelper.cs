using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;

namespace BetterLyrics.WinUI3.Helper
{
    public class ImageHelper
    {
        private static readonly ColorThief _colorThief = new();
        public const int AccentColorCount = 3;

        public static async Task<InMemoryRandomAccessStream> GetStreamFromBytesAsync(
            byte[] imageBytes
        )
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return null;

            InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(imageBytes.AsBuffer());

            return stream;
        }

        public static async Task<BitmapImage> GetBitmapImageFromBytesAsync(byte[] imageBytes)
        {
            var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(imageBytes.AsBuffer());
            stream.Seek(0);

            var bitmapImage = new BitmapImage();
            await bitmapImage.SetSourceAsync(stream);

            return bitmapImage;
        }

        public static async Task<InMemoryRandomAccessStream> ByteArrayToStream(byte[] bytes)
        {
            var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(bytes.AsBuffer());
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

        public static async Task<List<Color>> GetAccentColorsFromByte(byte[] bytes) =>
            [
                .. (
                    await _colorThief.GetPalette(await GetDecoderFromByte(bytes), AccentColorCount)
                ).Select(color =>
                    Color.FromArgb(color.Color.A, color.Color.R, color.Color.G, color.Color.B)
                ),
            ];

        public static async Task<BitmapDecoder> GetDecoderFromByte(byte[] bytes) =>
            await BitmapDecoder.CreateAsync(await ByteArrayToStream(bytes));
    }
}
