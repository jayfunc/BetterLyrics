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
        private const int _accentColorCount = 1;

        public static async Task<InMemoryRandomAccessStream> ByteArrayToStream(byte[] bytes)
        {
            var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(bytes.AsBuffer());
            stream.Seek(0);

            return stream;
        }

        public static RandomAccessStreamReference ByteArrayToRandomAccessStreamReference(byte[] bytes)
        {
            var stream = new InMemoryRandomAccessStream();
            var writer = new DataWriter(stream);
            writer.WriteBytes(bytes);
            writer.StoreAsync().GetAwaiter().GetResult();
            writer.FlushAsync().GetAwaiter().GetResult();
            writer.DetachStream();
            return RandomAccessStreamReference.CreateFromStream(stream);
        }

        public static async Task<byte[]> CreateTextPlaceholderBytesAsync(int width, int height)
        {
            var device = CanvasDevice.GetSharedDevice();
            var renderTarget = new CanvasRenderTarget(device, width, height, 96);

            // 随机生成渐变色
            Windows.UI.Color RandomColor()
            {
                var rand = new Random(Guid.NewGuid().GetHashCode());
                double h = rand.NextDouble() * 360;
                double s = 0.35 + rand.NextDouble() * 0.3; // 0.35~0.65，适中饱和度
                double l = 0.5 + rand.NextDouble() * 0.3;  // 0.5~0.8，明亮
                return HslToColor(h, s, l);
            }

            Windows.UI.Color color1 = RandomColor();
            Windows.UI.Color color2 = RandomColor();

            using (var ds = renderTarget.CreateDrawingSession())
            {
                // 绘制线性渐变背景
                var gradientBrush = new Microsoft.Graphics.Canvas.Brushes.CanvasLinearGradientBrush(ds, color1, color2)
                {
                    StartPoint = new System.Numerics.Vector2(0, 0),
                    EndPoint = new System.Numerics.Vector2(width, height)
                };
                ds.FillRectangle(0, 0, width, height, gradientBrush);
            }

            // 保存为 PNG 并转为 byte[]
            using (var stream = new InMemoryRandomAccessStream())
            {
                await renderTarget.SaveAsync(stream, CanvasBitmapFileFormat.Png);
                var buffer = new byte[stream.Size];
                using (var reader = new DataReader(stream.GetInputStreamAt(0)))
                {
                    await reader.LoadAsync((uint)stream.Size);
                    reader.ReadBytes(buffer);
                }
                return buffer;
            }

            // HSL转Color
            static Windows.UI.Color HslToColor(double h, double s, double l)
            {
                h = h / 360.0;
                double r = l, g = l, b = l;
                if (s != 0)
                {
                    double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
                    double p = 2 * l - q;
                    r = HueToRgb(p, q, h + 1.0 / 3.0);
                    g = HueToRgb(p, q, h);
                    b = HueToRgb(p, q, h - 1.0 / 3.0);
                }
                return Windows.UI.Color.FromArgb(255, (byte)(r * 255), (byte)(g * 255), (byte)(b * 255));
            }
            static double HueToRgb(double p, double q, double t)
            {
                if (t < 0) t += 1;
                if (t > 1) t -= 1;
                if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
                if (t < 1.0 / 2.0) return q;
                if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
                return p;
            }
        }

        public static List<Windows.UI.Color> GetAccentColorsFromByte(byte[] bytes)
        {
            // 使用 ImageSharp 读取图片
            using var image = SixLabors.ImageSharp.Image.Load<SixLabors.ImageSharp.PixelFormats.Rgba32>(bytes);

            // 简单聚类法：统计所有像素出现频率，取出现最多的前 AccentColorCount 个颜色
            var colorCount = new Dictionary<SixLabors.ImageSharp.PixelFormats.Rgba32, int>();

            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    var color = image[x, y];
                    // 可选：忽略透明像素
                    if (color.A < 32) continue;
                    if (colorCount.ContainsKey(color))
                        colorCount[color]++;
                    else
                        colorCount[color] = 1;
                }
            }

            // 按出现次数排序，取前 AccentColorCount 个
            var topColors = colorCount
                .OrderByDescending(kv => kv.Value)
                .Take(_accentColorCount)
                .Select(kv => kv.Key)
                .ToList();

            // 转换为 Windows.UI.Color
            return topColors
                .Select(c => Windows.UI.Color.FromArgb(c.A, c.R, c.G, c.B))
                .ToList();
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

            var themeColor = Rgba32.ParseHex(GetAccentColorsFromByte(imageBytes).FirstOrDefault().ToHex());

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
    }
}
