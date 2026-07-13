using System.Net;

namespace BetterLyrics.Core.Helpers;

public static class WebDavProbeHelper
{
    /// <summary>
    ///     自动检测目标主机是 HTTP 还是 HTTPS
    /// </summary>
    /// <returns>返回 "https" 或 "http"，如果都连不上返回 null</returns>
    public static async Task<string?> DetectSchemeAsync(string host, int port, string? path, string? user, string? pwd)
    {
        if (port == 443) return "https";
        if (port == 80) return "http";

        // 忽略 SSL 证书错误，因为很多 NAS 是自签名的
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
            UseProxy = false
        };

        // 设置认证
        if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pwd))
        {
            handler.Credentials = new NetworkCredential(user, pwd);
            handler.PreAuthenticate = true;
        }

        using var client = new HttpClient(handler);
        client.Timeout = TimeSpan.FromSeconds(3);

        if (await ProbeUrlAsync(client, "https", host, port, path)) return "https";

        if (await ProbeUrlAsync(client, "http", host, port, path)) return "http";

        // 都失败
        return null;
    }

    private static async Task<bool> ProbeUrlAsync(HttpClient client, string scheme, string host, int port, string? path)
    {
        try
        {
            var uriBuilder = new UriBuilder(scheme, host, port, path);

            // 使用 PROPFIND 方法，且 Depth 为 0，只检测根节点是否存在，不拉取列表
            var request = new HttpRequestMessage(new HttpMethod("PROPFIND"), uriBuilder.Uri);
            request.Headers.Add("Depth", "0");

            var response = await client.SendAsync(request);

            return response.StatusCode != HttpStatusCode.BadRequest;
        }
        catch
        {
            return false;
        }
    }
}