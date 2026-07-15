using System.Diagnostics;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Implementations.Services.FileSystemService.Providers;

public class LocalFileSystem : IUnifiedFileSystem
{
    private readonly MediaFolder _config;
    private readonly string _rootLocalPath;

    public LocalFileSystem(MediaFolder config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _rootLocalPath = config.UriPath;
    }

    public Task<bool> ConnectAsync()
    {
        var isExisted = Directory.Exists(_rootLocalPath);
        if (isExisted) return Task.FromResult(true);

        throw new FileNotFoundException(null, _rootLocalPath);
    }

    public async Task<List<FilesIndexItem>> GetFilesAsync(FilesIndexItem? parentFolder = null)
    {
        var result = new List<FilesIndexItem>();

        string targetPath;
        string parentUriString;

        try
        {
            if (parentFolder == null)
            {
                targetPath = _rootLocalPath;
                parentUriString = _config.GetStandardUri().AbsoluteUri;
            }
            else
            {
                var uri = new Uri(parentFolder.Uri);
                targetPath = uri.LocalPath;
                parentUriString = parentFolder.Uri;
            }

            if (!Directory.Exists(targetPath)) return result;

            var dirInfo = new DirectoryInfo(targetPath);

            foreach (var item in dirInfo.EnumerateFileSystemInfos())
            {
                // 跳过系统/隐藏文件
                if ((item.Attributes & FileAttributes.Hidden) != 0 ||
                    (item.Attributes & FileAttributes.System) != 0) continue;

                var isDir = (item.Attributes & FileAttributes.Directory) == FileAttributes.Directory;

                if (!isDir)
                {
                    var ext = item.Extension.ToLower();
                    // 过滤后缀名
                    if (string.IsNullOrEmpty(ext) || !FileHelper.AllSupportedExtensions.Contains(ext)) continue;
                }

                var itemUri = new Uri(item.FullName).AbsoluteUri;

                long size = 0;

                if (!isDir && item is FileInfo fi) size = fi.Length;

                result.Add(new FilesIndexItem
                {
                    MediaFolderId = _config.Id, // 关联配置 ID

                    ParentUri = parentUriString, // 记录父级 URI

                    Uri = itemUri,

                    FileName = item.Name,
                    IsDirectory = isDir,

                    FileSize = size,
                    DateCreated = item.CreationTime,
                    LastModified = item.LastWriteTime
                });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Local scan error: {ex.Message}");
        }

        return await Task.FromResult(result);
    }

    public async Task<Stream?> OpenReadAsync(FilesIndexItem entity)
    {
        if (entity == null) return null;

        var localPath = new Uri(entity.Uri).LocalPath;

        // 使用 FileShare.Read 允许其他程序同时读取
        // 使用 useAsync: true 优化异步读写性能
        return new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
    }

    public async Task DisconnectAsync()
    {
        await Task.CompletedTask;
    }

    public void Dispose()
    {
    }
}