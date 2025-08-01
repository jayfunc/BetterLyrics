// 2025/6/23 by Zhe Fang

using CommunityToolkit.WinUI.Helpers;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;

namespace BetterLyrics.WinUI3.Helper
{
    public class ImageHelper
    {
        public static async Task<InMemoryRandomAccessStream> ByteArrayToStream(byte[] bytes)
        {
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(bytes.AsBuffer());
            stream.Seek(0);

            return stream;
        }

        public static RandomAccessStreamReference ByteArrayToRandomAccessStreamReference(byte[] bytes)
        {
            using var stream = new InMemoryRandomAccessStream();
            using var writer = new DataWriter(stream);
            writer.WriteBytes(bytes);
            writer.StoreAsync().GetAwaiter().GetResult();
            writer.FlushAsync().GetAwaiter().GetResult();
            writer.DetachStream();
            return RandomAccessStreamReference.CreateFromStream(stream);
        }

        public static async Task<byte[]> CreateTextPlaceholderBytesAsync(int width, int height)
        {
            using var device = CanvasDevice.GetSharedDevice();
            using var renderTarget = new CanvasRenderTarget(device, width, height, 96);

            // 随机生成渐变色
            Windows.UI.Color RandomColor()
            {
                var rand = new Random(Guid.NewGuid().GetHashCode());
                double h = rand.NextDouble() * 360;
                double s = 0.35 + rand.NextDouble() * 0.3; // 0.35~0.65，适中饱和度
                double l = 0.5 + rand.NextDouble() * 0.3;  // 0.5~0.8，明亮
                return CommunityToolkit.WinUI.Helpers.ColorHelper.FromHsl(h, s, l);
            }

            Windows.UI.Color color1 = RandomColor();
            Windows.UI.Color color2 = RandomColor();

            using (var ds = renderTarget.CreateDrawingSession())
            {
                // 绘制线性渐变背景
                using var gradientBrush = new Microsoft.Graphics.Canvas.Brushes.CanvasLinearGradientBrush(ds, color1, color2)
                {
                    StartPoint = new Vector2(0, 0),
                    EndPoint = new Vector2(width, height)
                };
                ds.FillRectangle(0, 0, width, height, gradientBrush);
            }

            // 保存为 PNG 并转为 byte[]
            using var stream = new InMemoryRandomAccessStream();
            await renderTarget.SaveAsync(stream, CanvasBitmapFileFormat.Png);
            var buffer = new byte[stream.Size];
            using (var reader = new DataReader(stream.GetInputStreamAt(0)))
            {
                await reader.LoadAsync((uint)stream.Size);
                reader.ReadBytes(buffer);
            }
            return buffer;
        }

        public static List<Windows.UI.Color> GetAccentColorsFromByte(byte[] bytes, int count, bool? isDark = null)
        {
            using var image = Image.Load<Rgba32>(bytes);
            var colorThief = new ColorThief.ImageSharp.ColorThief();
            var mainColor = colorThief.GetColor(image, 10, false);
            var palette = colorThief.GetPalette(image, 255, 10, false);
            var topColors = palette
                .OrderByDescending(x => x.Population)
                .Where(x => x.IsDark == (isDark ?? mainColor.IsDark))
                .Select(x => Windows.UI.Color.FromArgb(x.Color.A, x.Color.R, x.Color.G, x.Color.B))
                .Take(count)
                .ToList();

            return topColors;
        }

        //public static async Task<BitmapImage> GetBitmapImageFromBytesAsync(byte[] imageBytes)
        //{
        //    var stream = new InMemoryRandomAccessStream();
        //    await stream.WriteAsync(imageBytes.AsBuffer());
        //    stream.Seek(0);

        //    var bitmapImage = new BitmapImage();
        //    await bitmapImage.SetSourceAsync(stream);

        //    return bitmapImage;
        //}

        //public static async Task<BitmapDecoder> GetDecoderFromByte(byte[] bytes) =>
        //    await BitmapDecoder.CreateAsync(await ByteArrayToStream(bytes));

        //public static async Task<InMemoryRandomAccessStream> GetStreamFromBytesAsync(byte[] imageBytes)
        //{
        //    if (imageBytes == null || imageBytes.Length == 0)
        //        return null;

        //    InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
        //    await stream.WriteAsync(imageBytes.AsBuffer());

        //    return stream;
        //}

        public static async Task<byte[]> ToByteArrayAsync(IRandomAccessStreamReference streamRef)
        {
            using IRandomAccessStream stream = await streamRef.OpenReadAsync();
            using var reader = new DataReader(stream);
            await reader.LoadAsync((uint)stream.Size);
            byte[] buffer = new byte[stream.Size];
            reader.ReadBytes(buffer);
            return buffer;
        }

        public static float GetAverageLuminance(CanvasBitmap bitmap)
        {
            var pixels = bitmap.GetPixelBytes();
            double sum = 0;
            for (int i = 0; i < pixels.Length; i += 4)
            {
                // BGRA
                byte b = pixels[i];
                byte g = pixels[i + 1];
                byte r = pixels[i + 2];
                // 忽略A
                double y = 0.299 * r + 0.587 * g + 0.114 * b;
                sum += y / 255.0;
            }
            return (float)(sum / (pixels.Length / 4));
        }

        public static byte[] MakeSquareWithThemeColor(byte[] imageBytes)
        {
            using var image = Image.Load<Rgba32>(imageBytes);

            if (image.Width == image.Height)
            {
                // 已经是正方形，直接返回
                return imageBytes;
            }

            int size = Math.Max(image.Width, image.Height);

            var themeColor = Rgba32.ParseHex(GetAccentColorsFromByte(imageBytes, 1).FirstOrDefault().ToHex());

            // 新建正方形画布
            using var square = new Image<Rgba32>(size, size, themeColor);

            // 计算居中位置
            int offsetX = (size - image.Width) / 2;
            int offsetY = (size - image.Height) / 2;

            // 绘制原图到正方形画布
            square.Mutate(ctx => ctx.DrawImage(image, new Point(offsetX, offsetY), 1f));

            // 保存为 PNG 字节流
            using var ms = new MemoryStream();
            square.Save(ms, new PngEncoder());
            return ms.ToArray();
        }

        public static byte[] Resize(byte[] imageBytes, int size)
        {
            using Image image = Image.Load(imageBytes);
            var factor = Math.Max(size / image.Width, size / image.Height);
            if (factor <= 1)
            {
                return imageBytes;
            }
            int width = image.Width * factor;
            int height = image.Height * factor;
            image.Mutate(x => x.Resize(width, height, KnownResamplers.Welch));

            using var ms = new MemoryStream();
            image.Save(ms, new PngEncoder());
            return ms.ToArray();
        }
    }
}
