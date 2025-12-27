using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.FileSystemService.Providers
{
    public partial class LocalFileSystem : IUnifiedFileSystem
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
            return Task.FromResult(Directory.Exists(_rootLocalPath));
        }

        public async Task<List<FileCacheEntity>> GetFilesAsync(FileCacheEntity? parentFolder = null)
        {
            var result = new List<FileCacheEntity>();

            string targetPath;
            string parentUriString;

            try
            {
                if (parentFolder == null)
                {
                    // 根目录
                    targetPath = _rootLocalPath;
                    parentUriString = _config.GetStandardUri().AbsoluteUri;
                }
                else
                {
                    // 子目录：从标准 URI (file:///...) 还原为本地路径 (C:\...)
                    var uri = new Uri(parentFolder.Uri);
                    targetPath = uri.LocalPath;
                    parentUriString = parentFolder.Uri;
                }

                if (!Directory.Exists(targetPath)) return result;

                var dirInfo = new DirectoryInfo(targetPath);

                foreach (var item in dirInfo.GetFileSystemInfos())
                {
                    // 生成标准 URI 作为唯一 ID
                    // new Uri("C:\Path\File") 会自动生成 file:///C:/Path/File
                    var itemUri = new Uri(item.FullName).AbsoluteUri;

                    bool isDir = (item.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
                    long size = 0;

                    // DirectoryInfo 没有 Length 属性，只有 FileInfo 有
                    if (!isDir && item is FileInfo fi)
                    {
                        size = fi.Length;
                    }

                    result.Add(new FileCacheEntity
                    {
                        MediaFolderId = _config.Id, // 关联配置 ID

                        ParentUri = parentUriString, // 记录父级 URI

                        Uri = itemUri, // 标准化 URI (file:///...)

                        FileName = item.Name,
                        IsDirectory = isDir,

                        FileSize = size,
                        LastModified = item.LastWriteTime
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Local scan error: {ex.Message}");
            }

            return await Task.FromResult(result);
        }

        public async Task<Stream?> OpenReadAsync(FileCacheEntity entity)
        {
            if (entity == null) return null;

            // 将标准 URI (file:///C:/...) 还原为本地路径 (C:\...)
            string localPath = new Uri(entity.Uri).LocalPath;

            // 使用 FileShare.Read 允许其他程序同时读取
            // 使用 useAsync: true 优化异步读写性能
            return new FileStream(localPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        }

        public async Task DisconnectAsync() => await Task.CompletedTask;
        public void Dispose() { }
    }
}