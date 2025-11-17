// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using Impressionist.Abstractions;
using Microsoft.Graphics.Canvas;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace BetterLyrics.WinUI3.Helper
{
    public class ImageHelper
    {
        public static async Task<IRandomAccessStream> GetAlbumArtPlaceholderAsync()
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(PathHelper.AlbumArtPlaceholderPath);
            IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read);
            return stream;
        }

        public static Task<ThemeColorResult> GetAccentColorAsync(BitmapDecoder decoder, PaletteGeneratorType generatorType)
        {
            return generatorType switch
            {
                PaletteGeneratorType.OctTree => PaletteHelper.OctTreeGetAccentColorFromByteAsync(decoder),
                PaletteGeneratorType.MedianCut => PaletteHelper.MedianCutGetAccentColorFromByteAsync(decoder),
                _ => throw new ArgumentOutOfRangeException(nameof(generatorType)),
            };
        }

        public static Task<PaletteResult> GetAccentColorsAsync(BitmapDecoder decoder, int count, PaletteGeneratorType generatorType, bool? isDark = null)
        {
            return generatorType switch
            {
                PaletteGeneratorType.OctTree => PaletteHelper.OctTreeGetAccentColorsFromByteAsync(decoder, count, isDark),
                PaletteGeneratorType.MedianCut => PaletteHelper.MedianCutGetAccentColorsFromByteAsync(decoder, count, isDark),
                _ => throw new ArgumentOutOfRangeException(nameof(generatorType)),
            };
        }

        public static async Task<IBuffer> ToBufferAsync(IRandomAccessStreamReference streamRef)
        {
            using IRandomAccessStream stream = await streamRef.OpenReadAsync();
            stream.Seek(0);
            var buffer = new Windows.Storage.Streams.Buffer((uint)stream.Size);
            await stream.ReadAsync(buffer, (uint)stream.Size, InputStreamOptions.None);
            return buffer;
        }

        public static async Task<BitmapDecoder> MakeSquareWithThemeColor(IBuffer buffer, PaletteGeneratorType generatorType)
        {
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(buffer);
            var decoder = await BitmapDecoder.CreateAsync(stream);

            if (decoder.PixelWidth == decoder.PixelHeight)
            {
                // 已经是正方形，直接返回
                return decoder;
            }

            using var device = CanvasDevice.GetSharedDevice();
            using var canvasBitmap = await CanvasBitmap.LoadAsync(device, stream);
            var size = Math.Max(decoder.PixelWidth, decoder.PixelHeight);

            var result = await GetAccentColorAsync(decoder, generatorType);
            var color = Windows.UI.Color.FromArgb(255, (byte)result.Color.X, (byte)result.Color.Y, (byte)result.Color.Z);
            using var renderTarget = new CanvasRenderTarget(device, size, size, 96);

            int offsetX = (int)(size - decoder.PixelWidth) / 2;
            int offsetY = (int)(size - decoder.PixelHeight) / 2;
            using (var ds = renderTarget.CreateDrawingSession())
            {
                ds.FillRectangle(0, 0, size, size, color);
                ds.DrawImage(canvasBitmap, offsetX, offsetY);
            }

            // 保存为 PNG 并转为 byte[]
            stream.Seek(0);
            stream.Size = 0;
            await renderTarget.SaveAsync(stream, CanvasBitmapFileFormat.Png);
            stream.Seek(0);
            var newDecoder = await BitmapDecoder.CreateAsync(stream);
            return newDecoder;

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
                else if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
                {
                    if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                    {
                        // 普通网络图片，下载
                        return await DownloadImageAsByteArrayAsync(url);
                    }
                    else if (uri.Scheme == Uri.UriSchemeFile)
                    {
                        // 本地文件，读取
                        var file = await StorageFile.GetFileFromPathAsync(uri.LocalPath);
                        var buffer = await FileIO.ReadBufferAsync(file);
                        return buffer.ToArray();
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static IRandomAccessStream ToIRandomAccessStream(IBuffer buffer)
        {
            return buffer.AsStream().AsRandomAccessStream();
        }
    }
}
