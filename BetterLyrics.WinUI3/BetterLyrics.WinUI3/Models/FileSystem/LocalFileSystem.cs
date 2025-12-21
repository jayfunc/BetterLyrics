using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models.FileSystem
{
    public partial class LocalFileSystem : IUnifiedFileSystem
    {
        private readonly string _rootPath;

        public LocalFileSystem(string rootPath)
        {
            _rootPath = rootPath;
        }

        public Task<bool> ConnectAsync()
        {
            return Task.FromResult(Directory.Exists(_rootPath));
        }

        public async Task<List<UnifiedFileItem>> GetFilesAsync(string relativePath)
        {
            var result = new List<UnifiedFileItem>();

            var targetPath = string.IsNullOrWhiteSpace(relativePath)
                ? _rootPath
                : Path.Combine(_rootPath, relativePath);

            if (!Directory.Exists(targetPath)) return result;

            var dirInfo = new DirectoryInfo(targetPath);

            foreach (var item in dirInfo.GetFileSystemInfos())
            {
                bool isDir = (item.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
                result.Add(new UnifiedFileItem
                {
                    Name = item.Name,
                    FullPath = item.FullName,
                    IsFolder = isDir,
                    Size = isDir ? 0 : ((FileInfo)item).Length,
                    LastModified = item.LastWriteTime
                });
            }
            return result;
        }

        public async Task<Stream> OpenReadAsync(string fullPath)
        {
            return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public async Task DisconnectAsync() => await Task.CompletedTask;
        public void Dispose() { }
    }
}
