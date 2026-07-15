using System.Diagnostics;
using System.Net;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Models.Settings;
using FluentFTP;

namespace BetterLyrics.Core.Implementations.Services.FileSystemService.Providers;

public class FTPFileSystem : IUnifiedFileSystem
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

        var port = _config.UriPort > 0 ? _config.UriPort : 0;

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
        if (_client.IsConnected) return true;
        await _client.AutoConnect(); // AutoConnect 会自动尝试 FTP/FTPS
        return _client.IsConnected;
    }

    public async Task<List<FilesIndexItem>> GetFilesAsync(FilesIndexItem? parentFolder = null)
    {
        var result = new List<FilesIndexItem>();

        string targetServerPath;
        Uri parentUri;

        if (parentFolder == null)
        {
            var rootUri = _config.GetStandardUri();
            targetServerPath = rootUri.AbsolutePath;
            parentUri = rootUri;
        }
        else
        {
            targetServerPath = GetServerPathFromUri(parentFolder.Uri);
            parentUri = new Uri(parentFolder.Uri);
        }

        targetServerPath = WebUtility.UrlDecode(targetServerPath).Replace("\\", "/");
        if (string.IsNullOrEmpty(targetServerPath)) targetServerPath = "/";

        try
        {
            var items = await _client.GetListing(targetServerPath, FtpListOption.Auto);

            var baseUriSchema = $"{parentUri.Scheme}://{parentUri.Host}";
            if (parentUri.Port > 0) baseUriSchema += $":{parentUri.Port}";

            foreach (var item in items)
            {
                // 跳过 . 和 .. 
                if (item.Name == "." || item.Name == "..") continue;

                // 只处理文件和文件夹
                if (item.Type != FtpObjectType.File && item.Type != FtpObjectType.Directory) continue;

                // 只处理特定后缀文件
                if (item.Type == FtpObjectType.File)
                {
                    var extension = Path.GetExtension(item.Name).ToLower();
                    if (string.IsNullOrEmpty(extension) ||
                        !FileHelper.AllSupportedExtensions.Contains(extension)) continue;
                }

                var builder = new UriBuilder(baseUriSchema)
                {
                    Path = item.FullName
                };

                result.Add(new FilesIndexItem
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
                    DateCreated = item.Created == DateTime.MinValue ? (item.Modified == DateTime.MinValue ? DateTime.Now : item.Modified) : item.Created,

                    IsMetadataParsed = false
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"FTP列表获取失败: {targetServerPath} - {ex.Message}");
        }

        return result;
    }

    public async Task<Stream?> OpenReadAsync(FilesIndexItem file)
    {
        if (file == null) return null;

        try
        {
            // 1. 还原服务器路径
            var serverPath = GetServerPathFromUri(file.Uri);

            // 2. 解码 (Uri 里的空格是 %20，FTP 需要真实空格)
            serverPath = WebUtility.UrlDecode(serverPath);

            // 3. 返回流
            // 注意：FluentFTP 的 OpenRead 依赖于连接保持活跃
            return await _client.OpenRead(serverPath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"打开文件流失败: {file.FileName} - {ex.Message}");
            return null;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_client.IsConnected) await _client.Disconnect();
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