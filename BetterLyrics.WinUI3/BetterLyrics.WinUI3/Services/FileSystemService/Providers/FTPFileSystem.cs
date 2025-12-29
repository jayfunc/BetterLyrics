using BetterLyrics.WinUI3.Models;
using FluentFTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net; // 用于 WebUtility.UrlDecode
using System.Text; // ★ 修复 Encoding 报错的关键
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

            var ftpConfig = new FtpConfig
            {
                ConnectTimeout = 5000,
                DataConnectionConnectTimeout = 5000,
                ReadTimeout = 10000,

                // 忽略证书错误
                ValidateAnyCertificate = true
            };

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
                if (_client.IsConnected) return true;
                await _client.AutoConnect(); // AutoConnect 会自动尝试 FTP/FTPS
                return _client.IsConnected;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FTP连接失败: {ex.Message}");
                return false;
            }
        }

        public async Task<List<FileCacheEntity>> GetFilesAsync(FileCacheEntity? parentFolder = null)
        {
            var result = new List<FileCacheEntity>();

            // 1. 确定 FTP 服务器上的绝对路径
            string targetServerPath;
            Uri parentUri;

            if (parentFolder == null)
            {
                // 根目录：从配置中提取
                var rootUri = _config.GetStandardUri();
                targetServerPath = rootUri.AbsolutePath;
                parentUri = rootUri;
            }
            else
            {
                // 子目录：从实体中提取
                targetServerPath = GetServerPathFromUri(parentFolder.Uri);
                parentUri = new Uri(parentFolder.Uri);
            }

            // 2. 路径清洗：解码 URL (比如 %20 -> 空格)，并统一分隔符
            targetServerPath = WebUtility.UrlDecode(targetServerPath).Replace("\\", "/");
            if (string.IsNullOrEmpty(targetServerPath)) targetServerPath = "/";

            try
            {
                // 3. 获取列表 (FluentFTP 自动处理列表解析)
                var items = await _client.GetListing(targetServerPath, FtpListOption.Auto);

                // 准备 Base URI Scheme (ftp://192.168.1.5:21) 用于拼接子项
                string baseUriSchema = $"{parentUri.Scheme}://{parentUri.Host}";
                if (parentUri.Port > 0) baseUriSchema += $":{parentUri.Port}";

                foreach (var item in items)
                {
                    // 跳过 . 和 .. 
                    if (item.Name == "." || item.Name == "..") continue;

                    // 只处理文件和文件夹
                    if (item.Type != FtpObjectType.File && item.Type != FtpObjectType.Directory) continue;

                    // 4. 构建标准 URI
                    // FluentFTP 的 item.FullName 通常是 "/Music/Song.mp3"
                    // 我们用 UriBuilder 把它封装成 "ftp://192.168.1.5:21/Music/Song.mp3"
                    // UriBuilder 会自动处理路径中的特殊字符编码
                    var builder = new UriBuilder(baseUriSchema)
                    {
                        Path = item.FullName
                    };

                    result.Add(new FileCacheEntity
                    {
                        MediaFolderId = _config.Id,
                        // 如果是根目录扫描，ParentUri 用 Config 的；否则用传入文件夹的
                        ParentUri = parentFolder?.Uri ?? _config.GetStandardUri().AbsoluteUri,

                        Uri = builder.Uri.AbsoluteUri, // 标准化 URI

                        FileName = item.Name,
                        IsDirectory = item.Type == FtpObjectType.Directory,
                        FileSize = item.Size,
                        // 防止某些服务器返回 MinValue
                        LastModified = item.Modified == DateTime.MinValue ? DateTime.Now : item.Modified,

                        IsMetadataParsed = false
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FTP列表获取失败: {targetServerPath} - {ex.Message}");
            }

            return result;
        }

        public async Task<Stream?> OpenReadAsync(FileCacheEntity file)
        {
            if (file == null) return null;

            try
            {
                // 1. 还原服务器路径
                string serverPath = GetServerPathFromUri(file.Uri);

                // 2. 解码 (Uri 里的空格是 %20，FTP 需要真实空格)
                serverPath = WebUtility.UrlDecode(serverPath);

                // 3. 返回流
                // 注意：FluentFTP 的 OpenRead 依赖于连接保持活跃
                return await _client.OpenRead(serverPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"打开文件流失败: {file.FileName} - {ex.Message}");
                return null;
            }
        }

        public async Task DisconnectAsync()
        {
            if (_client.IsConnected)
            {
                await _client.Disconnect();
            }
        }

        public void Dispose()
        {
            _client?.Dispose();
            GC.SuppressFinalize(this);
        }

        // 私有辅助方法
        private string GetServerPathFromUri(string uriString)
        {
            var uri = new Uri(uriString);
            return uri.AbsolutePath; // 这里拿到的比如是 "/Music/Song%201.mp3"
        }
    }
}