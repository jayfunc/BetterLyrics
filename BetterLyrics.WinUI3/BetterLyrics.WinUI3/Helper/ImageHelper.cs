// 2025/6/23 by Zhe Fang

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;

namespace BetterLyrics.WinUI3.Helper
{
    public class ImageHelper
    {
        public const int AccentColorCount = 3;

        public static async Task<InMemoryRandomAccessStream> ByteArrayToStream(byte[] bytes)
        {
            var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(bytes.AsBuffer());
            stream.Seek(0);

            return stream;
        }

        public static async Task<byte[]> CreateTextPlaceholderBytesAsync(string text, int width, int height)
        {
            var device = CanvasDevice.GetSharedDevice();
            var renderTarget = new CanvasRenderTarget(device, width, height, 96);

            // 居中绘制文字
            using (var ds = renderTarget.CreateDrawingSession())
            {
                // 背景色
                ds.Clear(Colors.LightGray);

                // 文字格式
                var format = new CanvasTextFormat
                {
                    FontSize = Math.Min(width, height) / 6f,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    HorizontalAlignment = CanvasHorizontalAlignment.Center,
                    VerticalAlignment = CanvasVerticalAlignment.Center,
                    WordWrapping = CanvasWordWrapping.Wrap,
                    TrimmingGranularity = CanvasTextTrimmingGranularity.Character,
                    Options = CanvasDrawTextOptions.Default,
                };

                // 设定边距
                float margin = Math.Min(width, height) / 12f;
                float availableWidth = width - 2 * margin;
                float availableHeight = height - 2 * margin;

                // 计算合适的字体大小以适应内容区域
                float fontSize = format.FontSize;
                float minFontSize = 8f;
                float maxFontSize = format.FontSize;
                CanvasTextLayout layout;
                do
                {
                    format.FontSize = fontSize;
                    layout = new CanvasTextLayout(
                        ds,
                        text,
                        format,
                        availableWidth,
                        availableHeight
                    );
                    if (
                        layout.LayoutBounds.Width <= availableWidth
                        && layout.LayoutBounds.Height <= availableHeight
                    )
                        break;
                    fontSize -= 1f;
                } while (fontSize >= minFontSize);

                // 居中绘制文字（在内容区域内居中）
                var bounds = layout.LayoutBounds;
                var x = margin + (availableWidth - (float)bounds.Width) / 2f - (float)bounds.X;
                var y = margin + (availableHeight - (float)bounds.Height) / 2f - (float)bounds.Y;
                ds.DrawTextLayout(layout, new Vector2(x, y), Colors.DarkGray);
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
                .Take(AccentColorCount)
                .Select(kv => kv.Key)
                .ToList();

            // 转换为 Windows.UI.Color
            return topColors
                .Select(c => Windows.UI.Color.FromArgb(c.A, c.R, c.G, c.B))
                .ToList();
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

        public static async Task<BitmapDecoder> GetDecoderFromByte(byte[] bytes) =>
            await BitmapDecoder.CreateAsync(await ByteArrayToStream(bytes));

        public static async Task<InMemoryRandomAccessStream> GetStreamFromBytesAsync(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                return null;

            InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(imageBytes.AsBuffer());

            return stream;
        }

        public static async Task<byte[]> ToByteArrayAsync(IRandomAccessStreamReference streamRef)
        {
            using IRandomAccessStream stream = await streamRef.OpenReadAsync();
            using var memoryStream = new MemoryStream();
            await stream.AsStreamForRead().CopyToAsync(memoryStream);
            return memoryStream.ToArray();
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
    }
}
