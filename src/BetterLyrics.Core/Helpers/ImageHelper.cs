// 2025/6/23 by Zhe Fang

using System.Text;

namespace BetterLyrics.Core.Helpers;

public class ImageHelper
{
    private static async Task<byte[]> DownloadImageAsByteArrayAsync(string url)
    {
        using var httpClient = new HttpClient();
        return await httpClient.GetByteArrayAsync(url);
    }

    private static byte[]? DataUrlToByteArray(string dataUrl)
    {
        const string base64Marker = ";base64,";
        var base64Index = dataUrl.IndexOf(base64Marker, StringComparison.OrdinalIgnoreCase);
        if (base64Index >= 0)
        {
            var base64Data = dataUrl.Substring(base64Index + base64Marker.Length);
            return Convert.FromBase64String(base64Data);
        }

        // 非 base64，直接取逗号后内容并解码
        var commaIndex = dataUrl.IndexOf(',');
        if (commaIndex >= 0)
        {
            var rawData = dataUrl.Substring(commaIndex + 1);
            return Encoding.UTF8.GetBytes(Uri.UnescapeDataString(rawData));
        }

        return null;
    }

    public static async Task<byte[]?> GetImageByteArrayFromUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        try
        {
            if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                // data URL，直接解析
                return DataUrlToByteArray(url);

            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                    // 普通网络图片，下载
                    return await DownloadImageAsByteArrayAsync(url);

                if (uri.Scheme == Uri.UriSchemeFile)
                    // 本地文件，读取
                    return await File.ReadAllBytesAsync(uri.LocalPath);

                return null;
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }
}