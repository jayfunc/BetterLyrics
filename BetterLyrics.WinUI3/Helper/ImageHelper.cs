// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using Impressionist.Abstractions;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
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

        public static Task<PaletteResult> GetAccentColorsAsync(BitmapDecoder decoder, int count, PaletteGeneratorType generatorType, bool isDark)
        {
            return generatorType switch
            {
                PaletteGeneratorType.OctTree => PaletteHelper.OctTreeGetAccentColorsFromByteAsync(decoder, count, isDark),
                PaletteGeneratorType.MedianCut => PaletteHelper.MedianCutGetAccentColorsFromByteAsync(decoder, count, isDark),
                PaletteGeneratorType.Auto => PaletteHelper.AutoGetAccentColorsFromByteAsync(decoder, count, isDark),
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

        public static async Task<BitmapDecoder> GetBitmapDecoderAsync(IBuffer buffer)
        {
            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(buffer);
            var decoder = await BitmapDecoder.CreateAsync(stream);

            return decoder;
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

        public static async Task<byte[]?> GetImageByteArrayFromUrlAsync(string url)
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

        public static byte[] ToByteArray(IBuffer buffer)
        {
            using (var dataReader = DataReader.FromBuffer(buffer))
            {
                byte[] byteArray = new byte[buffer.Length];
                dataReader.ReadBytes(byteArray);
                return byteArray;
            }
        }

        public static IRandomAccessStream ToIRandomAccessStream(byte[] arr)
        {
            MemoryStream stream = new MemoryStream(arr);
            return stream.AsRandomAccessStream();
        }
    }
}
