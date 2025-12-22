using SMBLibrary;
using SMBLibrary.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Models.FileSystem
{
    public partial class SMBFileSystem : IUnifiedFileSystem
    {
        private SMB2Client _client;
        private ISMBFileStore _fileStore;

        private readonly string _ip;
        private readonly string _shareName;
        private readonly string _pathInsideShare; // 共享里的子路径
        private readonly string _username;
        private readonly string _password;

        // fullPathInput 例如: "192.168.1.5/Music/Pop"
        public SMBFileSystem(string fullPathInput, string user, string pass)
        {
            _username = user;
            _password = pass;

            // 解析路径：分离 IP 和 共享名
            var parts = fullPathInput.Replace("\\", "/").Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length >= 1) _ip = parts[0];
            if (parts.Length >= 2) _shareName = parts[1];

            // 剩下的部分重新拼起来作为子路径
            if (parts.Length > 2)
                _pathInsideShare = string.Join("\\", parts.Skip(2));
            else
                _pathInsideShare = "";
        }

        public async Task<bool> ConnectAsync()
        {
            _client = new SMB2Client();
            bool connected = _client.Connect(_ip, SMBTransportType.DirectTCPTransport);
            if (!connected) return false;

            var status = _client.Login(string.Empty, _username, _password);
            if (status != NTStatus.STATUS_SUCCESS) return false;

            // 连接具体的共享文件夹
            if (string.IsNullOrEmpty(_shareName)) return true; // 只连了服务器，没连共享

            _fileStore = _client.TreeConnect(_shareName, out status);
            return status == NTStatus.STATUS_SUCCESS;
        }

        public async Task<List<UnifiedFileItem>> GetFilesAsync(string relativePath)
        {
            var result = new List<UnifiedFileItem>();
            if (_fileStore == null) return result;

            // 拼接完整路径: Root里面的子路径 + 传入的相对路径
            string queryPath = Path.Combine(_pathInsideShare, relativePath).Replace("/", "\\").TrimStart('\\');

            // 打开目录
            var statusRet = _fileStore.CreateFile(out object handle, out FileStatus status, queryPath,
                AccessMask.GENERIC_READ, SMBLibrary.FileAttributes.Directory, ShareAccess.Read,
                CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);

            if (statusRet != NTStatus.STATUS_SUCCESS) return result;

            List<QueryDirectoryFileInformation> fileInfo;
            do
            {
                statusRet = _fileStore.QueryDirectory(out fileInfo, handle, "*", FileInformationClass.FileDirectoryInformation);

                List<FileDirectoryInformation> list = fileInfo.Select(x => (FileDirectoryInformation)x).ToList();
                foreach (var item in list)
                {
                    // 排除当前目录和父目录
                    if (item.FileName == "." || item.FileName == "..") continue;

                    result.Add(new UnifiedFileItem
                    {
                        Name = item.FileName,
                        FullPath = Path.Combine(queryPath, item.FileName),
                        IsFolder = (item.FileAttributes & SMBLibrary.FileAttributes.Directory) == SMBLibrary.FileAttributes.Directory,
                        Size = item.AllocationSize,
                        LastModified = item.LastWriteTime
                    });
                }

                if (statusRet == NTStatus.STATUS_NO_MORE_FILES)
                {
                    break;
                }

                if (statusRet != NTStatus.STATUS_SUCCESS)
                {
                    // Log
                    break;
                }
            } while (statusRet == NTStatus.STATUS_SUCCESS);

            _fileStore.CloseFile(handle);
            return result;
        }

        public async Task<Stream> OpenReadAsync(string fullPath)
        {
            var ret = _fileStore.CreateFile(out object handle, out FileStatus status, fullPath,
                AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE, 0, ShareAccess.Read, CreateDisposition.FILE_OPEN, 0, null);

            if (ret != NTStatus.STATUS_SUCCESS) throw new IOException($"SMB Open Error: {ret}");

            return new SMBReadOnlyStream(_fileStore, handle);
        }

        public async Task DisconnectAsync()
        {
            _client?.Disconnect();
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            _client?.Disconnect();
        }
    }
}
