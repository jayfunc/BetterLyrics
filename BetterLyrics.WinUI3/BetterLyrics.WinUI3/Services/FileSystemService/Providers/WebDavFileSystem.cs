using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebDav;

namespace BetterLyrics.WinUI3.Services.FileSystemService.Providers
{
    public partial class WebDavFileSystem : IUnifiedFileSystem
    {
        private readonly WebDavClient _client;
        private readonly MediaFolder _config;
        private readonly Uri _baseAddress;

        public WebDavFileSystem(MediaFolder config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            // 1. 构建 BaseAddress (只包含 http://host:port/)
            // MediaFolder.GetStandardUri() 返回的是带路径的完整 URI (http://host:port/path)
            // 我们需要提取出根用于初始化 WebDavClient
            var fullUri = _config.GetStandardUri();

            // 提取 "http://host:port"
            _baseAddress = new Uri($"{fullUri.Scheme}://{fullUri.Authority}");

            _client = new WebDavClient(new WebDavClientParams
            {
                BaseAddress = _baseAddress,
                Credentials = new System.Net.NetworkCredential(_config.UserName, _config.Password)
            });
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                // 测试连接：Propfind 请求配置的根路径
                // GetStandardUri 已经包含了用户设置的路径
                var result = await _client.Propfind(_config.GetStandardUri().AbsoluteUri);
                return result.IsSuccessful;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<FileCacheEntity>> GetFilesAsync(FileCacheEntity? parentFolder = null)
        {
            var list = new List<FileCacheEntity>();

            // 1. 确定目标 URI
            Uri targetUri;
            if (parentFolder == null)
            {
                targetUri = _config.GetStandardUri();
            }
            else
            {
                targetUri = new Uri(parentFolder.Uri);
            }

            // 2. 发送请求 (使用绝对 URI)
            // WebDavClient 允许传入绝对路径，它会自动处理
            var result = await _client.Propfind(targetUri.AbsoluteUri);

            if (result.IsSuccessful)
            {
                // 3. 准备父级 URI 字符串 (用于填充 Entity)
                // 确保以 / 结尾，方便后续逻辑判断或数据库查询
                string parentUriString = targetUri.AbsoluteUri;
                if (!parentUriString.EndsWith("/")) parentUriString += "/";

                // WebDAV 可能会把文件夹自己作为结果返回，我们需要过滤它
                // 比较时忽略末尾斜杠
                string targetPathClean = targetUri.AbsolutePath.TrimEnd('/');

                foreach (var res in result.Resources)
                {
                    // res.Uri 通常是相对路径，例如 "/dav/music/file.mp3"
                    // 我们需要将其转换为绝对 URI
                    var itemUri = new Uri(_baseAddress, res.Uri);

                    // ★ 过滤掉文件夹自身
                    // 比较 AbsolutePath (例如 /dav/music vs /dav/music)
                    if (itemUri.AbsolutePath.TrimEnd('/') == targetPathClean) continue;

                    // 获取文件名 (解码)
                    // res.DisplayName 有时候是空的，这时候需要从 Uri 解析
                    string name = res.DisplayName;
                    if (string.IsNullOrEmpty(name))
                    {
                        // 取最后一段，忽略末尾斜杠
                        name = itemUri.AbsolutePath.TrimEnd('/').Split('/').Last();
                        name = System.Net.WebUtility.UrlDecode(name);
                    }

                    if (string.IsNullOrEmpty(name)) continue;

                    list.Add(new FileCacheEntity
                    {
                        MediaFolderId = _config.Id,

                        // 记录父级 URI (保持传入时的形式，或者统一标准)
                        // 注意：对于 WebDAV，ParentUri 最好不带末尾斜杠，除非是根
                        ParentUri = parentFolder?.Uri ?? _config.GetStandardUri().AbsoluteUri,

                        // ★ 存储完整的 http://... 标准 URI
                        Uri = itemUri.AbsoluteUri,

                        FileName = name,
                        IsDirectory = res.IsCollection,

                        // WebDAV 通常能提供这些信息
                        FileSize = res.ContentLength ?? 0,
                        LastModified = res.LastModifiedDate
                    });
                }
            }

            return list;
        }

        public async Task<Stream?> OpenReadAsync(FileCacheEntity entity)
        {
            if (entity == null) return null;

            // WebDAV 获取流，直接使用完整 URI
            var res = await _client.GetRawFile(entity.Uri);

            if (!res.IsSuccessful)
                throw new IOException($"WebDAV Error {res.StatusCode}: {res.Description}");

            return res.Stream;
        }

        public async Task DisconnectAsync() => await Task.CompletedTask;

        public void Dispose() => _client?.Dispose();
    }
}