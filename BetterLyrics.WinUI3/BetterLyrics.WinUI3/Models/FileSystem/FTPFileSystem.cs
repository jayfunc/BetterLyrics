using FluentFTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models.FileSystem
{
    public partial class FTPFileSystem : IUnifiedFileSystem
    {
        private readonly AsyncFtpClient _client;
        private readonly string _rootPath; // 服务器上的根路径 (例如 /pub/music)

        public FTPFileSystem(string host, string user, string pass, int port, string remotePath)
        {
            // 如果 path 是 "192.168.1.5/Music"，我们需要把 /Music 拆出来
            // 但为了简单，假设 host 仅仅是 IP，remotePath 才是路径
            _rootPath = remotePath ?? "/";

            var config = new FtpConfig { ConnectTimeout = 5000 };
            _client = new AsyncFtpClient(host, user ?? "anonymous", pass ?? "", port > 0 ? port : 21, config);
        }

        public async Task<bool> ConnectAsync()
        {
            await _client.AutoConnect();
            return _client.IsConnected;
        }

        public async Task<List<UnifiedFileItem>> GetFilesAsync(string relativePath)
        {
            string targetPath = Path.Combine(_rootPath, relativePath).Replace("\\", "/");

            var items = await _client.GetListing(targetPath);
            return items.Select(i => new UnifiedFileItem
            {
                Name = i.Name,
                FullPath = i.FullName,
                IsFolder = i.Type == FtpObjectType.Directory,
                Size = i.Size,
                LastModified = i.Modified
            }).ToList();
        }

        public async Task<Stream> OpenReadAsync(string fullPath)
        {
            return await _client.OpenRead(fullPath);
        }

        public async Task DisconnectAsync() => await _client.Disconnect();
        public void Dispose() => _client?.Dispose();
    }
}
