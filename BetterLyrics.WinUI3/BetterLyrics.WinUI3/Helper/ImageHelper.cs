// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models.Settings;
using CommunityToolkit.WinUI.Helpers;
using Impressionist.Abstractions;
using Impressionist.Implementations;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media.Imaging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;
using static Vanara.PInvoke.Ole32;

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

        
        public static Task<ThemeColorResult> GetAccentColorFromByteAsync(byte[] bytes, PaletteGeneratorType generatorType)
        {
            return generatorType switch
            {
                PaletteGeneratorType.OctTree => PaletteHelper.OctTreeGetAccentColorFromByteAsync(bytes),
                PaletteGeneratorType.MedianCut => PaletteHelper.MedianCutGetAccentColorFromByteAsync(bytes),
                _ => throw new ArgumentOutOfRangeException("generatorType"),
            };
        }
        public static Task<PaletteResult> GetAccentColorsFromByteAsync(byte[] bytes, int count, PaletteGeneratorType generatorType, bool? isDark = null)
        {
            return generatorType switch
            {
                PaletteGeneratorType.OctTree => PaletteHelper.OctTreeGetAccentColorsFromByteAsync(bytes, count, isDark),
                PaletteGeneratorType.MedianCut => PaletteHelper.MedianCutGetAccentColorsFromByteAsync(bytes, count, isDark),
                _ => throw new ArgumentOutOfRangeException("generatorType"),
            };
        }

        public static async Task<Dictionary<Vector3, int>> GetPixelColor(BitmapDecoder bitmapDecoder)
        {
            var pixelDataProvider = await bitmapDecoder.GetPixelDataAsync();
            var pixels = pixelDataProvider.DetachPixelData();
            var count = bitmapDecoder.PixelWidth * bitmapDecoder.PixelHeight;
            var vector = new Dictionary<Vector3, int>();
            for (int i = 0; i < count; i += 10)
            {
                var offset = i * 4;
                var b = pixels[offset];
                var g = pixels[offset + 1];
                var r = pixels[offset + 2];
                var a = pixels[offset + 3];
                if (a == 0) continue;
                var color = new Vector3(r, g, b);
                if (vector.ContainsKey(color))
                {
                    vector[color]++;
                }
                else
                {
                    vector[color] = 1;
                }
            }
            return vector;
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

        public static double GetAverageLuminance(CanvasBitmap bitmap)
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
            return (double)(sum / (pixels.Length / 4));
        }

        public static async Task<byte[]> MakeSquareWithThemeColor(byte[] imageBytes, PaletteGeneratorType generatorType)
        {
            using var image = Image.Load<Rgba32>(imageBytes);

            if (image.Width == image.Height)
            {
                // 已经是正方形，直接返回
                return imageBytes;
            }

            int size = Math.Max(image.Width, image.Height);

            var result = await GetAccentColorFromByteAsync(imageBytes, generatorType);
            var color = Windows.UI.Color.FromArgb(255, (byte)result.Color.X, (byte)result.Color.Y, (byte)result.Color.Z);
            var themeColor = Rgba32.ParseHex(color.ToHex());

            using var square = new Image<Rgba32>(size, size, themeColor);

            int offsetX = (size - image.Width) / 2;
            int offsetY = (size - image.Height) / 2;

            square.Mutate(ctx => ctx.DrawImage(image, new Point(offsetX, offsetY), 1f));

            using var ms = new MemoryStream();
            square.Save(ms, new PngEncoder());
            return ms.ToArray();
        }

        public static byte[] Resize(byte[] imageBytes, int size)
        {
            using (Image image = Image.Load(imageBytes))
            {
                var factor = Math.Max((double)size / image.Width, (double)size / image.Height);

                int width = (int)(image.Width * factor);
                int height = (int)(image.Height * factor);

                if (factor > 1)
                {
                    image.Mutate(x => x.Resize(width, height, KnownResamplers.Welch));
                }
                else
                {
                    image.Mutate(x => x.Resize(width, height, KnownResamplers.NearestNeighbor));
                }

                using var ms = new MemoryStream();
                image.Save(ms, new JpegEncoder());
                return ms.ToArray();
            }
        }

        public static byte[] GenerateNoiseBGRA(int width, int height)
        {
            var random = new Random();
            var pixelData = new byte[width * height * 4];
            for (int i = 0; i < width * height; i++)
            {
                byte gray = (byte)random.Next(0, 256);
                pixelData[i * 4 + 0] = gray; // B
                pixelData[i * 4 + 1] = gray; // G
                pixelData[i * 4 + 2] = gray; // R
                pixelData[i * 4 + 3] = 255;  // A
            }
            return pixelData;
        }

        public static async Task<byte[]> DownloadImageAsByteArrayAsync(string url)
        {
            using var httpClient = new HttpClient();
            return await httpClient.GetByteArrayAsync(url);
        }

        public static byte[]? DataUrlToByteArray(string dataUrl)
        {
            const string base64Marker = ";base64,";
            int base64Index = dataUrl.IndexOf(base64Marker, StringComparison.OrdinalIgnoreCase);
            if (base64Index >= 0)
            {
                string base64Data = dataUrl.Substring(base64Index + base64Marker.Length);
                return Convert.FromBase64String(base64Data);
            }
            else
            {
                // 非 base64，直接取逗号后内容并解码
                int commaIndex = dataUrl.IndexOf(',');
                if (commaIndex >= 0)
                {
                    string rawData = dataUrl.Substring(commaIndex + 1);
                    return System.Text.Encoding.UTF8.GetBytes(Uri.UnescapeDataString(rawData));
                }
                else
                {
                    return null;
                }
            }
        }

        public static async Task<byte[]?> GetImageBytesFromUrlAsync(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            try
            {
                if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                {
                    // data URL，直接解析
                    return DataUrlToByteArray(url);
                }
                else if (Uri.TryCreate(url, UriKind.Absolute, out var uri) &&
                         (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    // 普通网络图片，下载
                    return await DownloadImageAsByteArrayAsync(url);
                }
                else
                {
                    // 其他类型暂不支持
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
