using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebDav;

namespace BetterLyrics.WinUI3.Models.FileSystem
{
    public partial class WebDavFileSystem : IUnifiedFileSystem
    {
        private readonly WebDavClient _client;
        private readonly string _baseUrl;
        private readonly string _rootPath;

        // host: http://192.168.1.5:5005
        // path: /music
        public WebDavFileSystem(string host, string user, string pass, int port, string path)
        {
            if (!host.StartsWith("http")) host = $"http://{host}";
            if (port > 0) host = $"{host}:{port}";

            _baseUrl = host;
            _rootPath = path ?? "/";

            _client = new WebDavClient(new WebDavClientParams
            {
                BaseAddress = new Uri(_baseUrl),
                Credentials = new System.Net.NetworkCredential(user, pass)
            });
        }

        public async Task<bool> ConnectAsync()
        {
            // WebDAV 无状态，Propfind 测试根目录连通性
            var result = await _client.Propfind(_rootPath);
            return result.IsSuccessful;
        }

        public async Task<List<UnifiedFileItem>> GetFilesAsync(string relativePath)
        {
            var targetPath = Path.Combine(_rootPath, relativePath).Replace("\\", "/");
            var result = await _client.Propfind(targetPath);

            var list = new List<UnifiedFileItem>();
            if (result.IsSuccessful)
            {
                foreach (var res in result.Resources)
                {
                    if (res == null || res.Uri == null) continue;

                    // 排除掉文件夹自身 (WebDAV 通常会把当前请求的文件夹作为第一个结果返回)
                    // 通过判断 URL 结尾是否一致来简单过滤，或者判断 IsCollection 且 Uri 相同
                    // 这里简单处理：只要名字不为空
                    var name = System.Net.WebUtility.UrlDecode(res.Uri.Split('/').LastOrDefault());
                    if (string.IsNullOrEmpty(name)) continue;

                    // 如果名字和请求的目录名一样，可能是它自己，跳过 (这需要根据具体服务器响应调整)
                    // 更稳妥的是比较 Uri

                    list.Add(new UnifiedFileItem
                    {
                        Name = name,
                        FullPath = res.Uri.ToString(), // WebDAV 需要完整 URI 
                        IsFolder = res.IsCollection,
                        Size = res.ContentLength ?? 0,
                        LastModified = res.LastModifiedDate
                    });
                }
            }
            return list;
        }

        public async Task<Stream> OpenReadAsync(string fullPath)
        {
            // WebDAV 获取流
            var res = await _client.GetRawFile(fullPath);
            if (!res.IsSuccessful) throw new IOException($"WebDAV Error: {res.StatusCode}");
            return res.Stream;
        }

        public async Task DisconnectAsync() => await Task.CompletedTask;

        public void Dispose() => _client?.Dispose();
    }
}
