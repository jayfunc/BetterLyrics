using BetterLyrics.WinUI3.Models;
using FluentFTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.FileSystemService.Providers
{
    public partial class FTPFileSystem : IUnifiedFileSystem
    {
        private readonly AsyncFtpClient _client;
        private readonly MediaFolder _config;

        public FTPFileSystem(MediaFolder config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            // 初始化 FluentFTP 配置
            var ftpConfig = new FtpConfig
            {
                ConnectTimeout = 5000,
                // 根据需要配置编码，防止中文乱码
                // Encoding = System.Text.Encoding.GetEncoding("GB2312") 
            };

            // FluentFTP 构造函数接收主机、用户、密码、端口
            // 端口如果为 -1 (MediaFolder 默认值)，则让 FluentFTP 使用默认 21
            int port = _config.UriPort > 0 ? _config.UriPort : 0;

            _client = new AsyncFtpClient(
                _config.UriHost,
                _config.UserName ?? "anonymous",
                _config.Password ?? "",
                port,
                ftpConfig
            );
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                await _client.AutoConnect();
                return _client.IsConnected;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<FileCacheEntity>> GetFilesAsync(FileCacheEntity? parentFolder = null)
        {
            var result = new List<FileCacheEntity>();

            // 1. 确定目标服务器路径
            string targetServerPath;
            Uri parentUri;

            if (parentFolder == null)
            {
                // 根目录：从配置中提取路径 (例如 /Music)
                // GetStandardUri().AbsolutePath 会返回带前导斜杠的路径
                var rootUri = _config.GetStandardUri();
                targetServerPath = rootUri.AbsolutePath; // "/Music"
                parentUri = rootUri;
            }
            else
            {
                // 子目录：将标准 URI 转换为 FTP 服务器路径
                targetServerPath = GetServerPathFromUri(parentFolder.Uri);
                parentUri = new Uri(parentFolder.Uri);
            }

            // 确保路径合法性 (FluentFTP 喜欢 Unix 风格斜杠)
            targetServerPath = targetServerPath.Replace("\\", "/");
            if (string.IsNullOrEmpty(targetServerPath)) targetServerPath = "/";

            // 2. 获取列表
            var items = await _client.GetListing(targetServerPath);

            // 3. 准备 Base URI 用于拼接子项
            // FTP URI 基础部分: ftp://host:port
            string baseUriStr = $"{parentUri.Scheme}://{parentUri.Host}";
            if (parentUri.Port > 0) baseUriStr += $":{parentUri.Port}";

            foreach (var item in items)
            {
                // 排除 . 和 ..
                if (item.Name == "." || item.Name == "..") continue;

                // 构建完整的标准 URI
                // item.FullName 是服务器上的绝对路径 (例如 /Music/Song.mp3)
                // 我们需要把它拼成 ftp://host:port/Music/Song.mp3
                // 注意：Path.Combine 在 Windows 上可能会用反斜杠，这里手动拼接更安全

                string itemFullPath = item.FullName.StartsWith("/") ? item.FullName : "/" + item.FullName;
                string standardUri = baseUriStr + itemFullPath; // Uri 构造函数会自动处理编码

                result.Add(new FileCacheEntity
                {
                    MediaFolderId = _config.Id,

                    // 记录父级 URI
                    // 如果 parentFolder 为空，则父级是 Config 的根 URI
                    ParentUri = parentFolder?.Uri ?? _config.GetStandardUri().AbsoluteUri,

                    Uri = standardUri, // 标准化 URI

                    FileName = item.Name,
                    IsDirectory = item.Type == FtpObjectType.Directory,

                    FileSize = item.Size,
                    LastModified = item.Modified
                });
            }

            return result;
        }

        public async Task<Stream?> OpenReadAsync(FileCacheEntity entity)
        {
            if (entity == null) return null;

            // 从标准 URI 还原回 FTP 服务器路径
            string serverPath = GetServerPathFromUri(entity.Uri);

            return await _client.OpenRead(serverPath);
        }

        public async Task DisconnectAsync() => await _client.Disconnect();
        public void Dispose() => _client?.Dispose();

        // =========================================================
        // ★ 私有辅助方法：URI -> FTP Path
        // =========================================================
        private string GetServerPathFromUri(string uriString)
        {
            // 输入: ftp://192.168.1.5:21/Music/Song.mp3
            // 输出: /Music/Song.mp3

            var uri = new Uri(uriString);

            // Uri.AbsolutePath 自动包含了路径部分 (例如 /Music/Song.mp3)
            // 并且会自动进行 URL Decode (比如 %20 -> 空格)
            // 这正是 FluentFTP 需要的格式
            return uri.AbsolutePath;
        }
    }
}