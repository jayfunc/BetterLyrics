using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using SMBLibrary;
using SMBLibrary.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.FileSystemService.Providers
{
    public partial class SMBFileSystem : IUnifiedFileSystem
    {
        private SMB2Client? _client;
        private ISMBFileStore? _fileStore;

        // 保存配置对象的引用，它是我们的“真理来源”
        private readonly MediaFolder _config;

        // 缓存解析出来的 Share 名称，因为 TreeConnect 要用
        private string _shareName;

        public SMBFileSystem(MediaFolder config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));

            // 在构造时就解析好 Share 名称，避免后续重复解析
            // 假设 URI 是 smb://host/ShareName/Folder/Sub
            // 我们需要提取 "ShareName"
            var uri = _config.GetStandardUri();

            // Segments[0] 是 "/", Segments[1] 是 "ShareName/"
            if (uri.Segments.Length > 1)
            {
                _shareName = uri.Segments[1].TrimEnd('/');
            }
            else
            {
                // 如果没有 ShareName，这在 SMB 中通常是不合法的，但在根目录下可能发生
                _shareName = "";
            }
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                _client = new SMB2Client();

                // 1. 连接主机
                bool connected = _client.Connect(_config.UriHost, SMBTransportType.DirectTCPTransport);
                if (!connected) return false;

                // 2. 登录
                var status = _client.Login(string.Empty, _config.UserName, _config.Password);
                if (status != NTStatus.STATUS_SUCCESS) return false;

                // 3. 连接共享目录 (TreeConnect)
                // 注意：SMBLibrary 必须先连接到 Share，后续所有文件操作都是基于这个 Share 的相对路径
                if (string.IsNullOrEmpty(_shareName)) return false;

                _fileStore = _client.TreeConnect(_shareName, out status);
                return status == NTStatus.STATUS_SUCCESS;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 获取文件列表
        /// </summary>
        /// <param name="parentFolder">
        /// 传入要列出的文件夹实体。
        /// 如果传入 null，则默认列出 MediaFolder 配置的根目录。
        /// </param>
        public async Task<List<FileCacheEntity>> GetFilesAsync(FileCacheEntity? parentFolder = null)
        {
            var result = new List<FileCacheEntity>();
            if (_fileStore == null) return result;

            string smbPath = GetPathRelativeToShare(parentFolder);

            var statusRet = _fileStore.CreateFile(out object handle, out FileStatus status, smbPath,
                AccessMask.GENERIC_READ, SMBLibrary.FileAttributes.Directory, ShareAccess.Read,
                CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);

            if (statusRet != NTStatus.STATUS_SUCCESS) return result;

            string parentUriString = parentFolder?.Uri ?? _config.GetStandardUri().AbsoluteUri;

            List<QueryDirectoryFileInformation> fileInfo;

            do
            {
                statusRet = _fileStore.QueryDirectory(out fileInfo, handle, "*", FileInformationClass.FileDirectoryInformation);

                // 如果查询失败或者没有更多文件，fileInfo 可能是 null，直接跳出
                if (statusRet != NTStatus.STATUS_SUCCESS && statusRet != NTStatus.STATUS_NO_MORE_FILES)
                {
                    break;
                }

                // 如果是 NO_MORE_FILES 但 fileInfo 依然有残留数据（极少见），或者是 SUCCESS
                if (fileInfo != null)
                {
                    foreach (var item in fileInfo.Cast<FileDirectoryInformation>())
                    {
                        if (item.FileName == "." || item.FileName == "..") continue;

                        // 过滤隐藏文件和系统文件
                        if ((item.FileAttributes & SMBLibrary.FileAttributes.Hidden) == SMBLibrary.FileAttributes.Hidden ||
                        (item.FileAttributes & SMBLibrary.FileAttributes.System) == SMBLibrary.FileAttributes.System)
                        {
                            continue;
                        }

                        bool isDir = (item.FileAttributes & SMBLibrary.FileAttributes.Directory) == SMBLibrary.FileAttributes.Directory;

                        // 后缀名过滤
                        if (!isDir)
                        {
                            string extension = Path.GetExtension(item.FileName);
                            if (string.IsNullOrEmpty(extension) || !FileHelper.AllSupportedExtensions.Contains(extension)) continue;
                        }

                        if (!parentUriString.EndsWith("/")) parentUriString += "/";
                        var baseUri = new Uri(parentUriString);
                        var newUri = new Uri(baseUri, item.FileName);

                        result.Add(new FileCacheEntity
                        {
                            MediaFolderId = _config.Id,
                            ParentUri = parentFolder?.Uri ?? _config.GetStandardUri().AbsoluteUri,

                            Uri = newUri.AbsoluteUri,

                            FileName = item.FileName,
                            IsDirectory = (item.FileAttributes & SMBLibrary.FileAttributes.Directory) == SMBLibrary.FileAttributes.Directory,
                            FileSize = item.AllocationSize,
                            LastModified = item.ChangeTime
                        });
                    }
                }

                if (statusRet == NTStatus.STATUS_NO_MORE_FILES) break;

            } while (statusRet == NTStatus.STATUS_SUCCESS);

            _fileStore.CloseFile(handle);
            return result;
        }

        /// <summary>
        /// 打开文件流
        /// </summary>
        /// <param name="file">只需要传入文件实体即可</param>
        public async Task<Stream?> OpenReadAsync(FileCacheEntity file)
        {
            if (_fileStore == null || file == null) return null;

            // ★ 核心简化：直接把对象扔进去，获取路径
            string smbPath = GetPathRelativeToShare(file);

            var ret = _fileStore.CreateFile(out object handle, out FileStatus status, smbPath,
                AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE, 0, ShareAccess.Read, CreateDisposition.FILE_OPEN, 0, null);

            if (ret != NTStatus.STATUS_SUCCESS)
                throw new IOException($"SMB Open Error: {ret}");

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

        // =========================================================
        // ★ 私有魔法方法：处理所有令人头大的路径逻辑
        // =========================================================
        private string GetPathRelativeToShare(FileCacheEntity? entity)
        {
            Uri targetUri;

            if (entity == null)
            {
                targetUri = _config.GetStandardUri();
            }
            else
            {
                targetUri = new Uri(entity.Uri);
            }

            // 1. 获取绝对路径
            // ★★★ 关键修正：必须解码！把 %20 变回空格 ★★★
            // targetUri.AbsolutePath -> "/Share/My%20Music/Song.mp3"
            // Uri.UnescapeDataString -> "/Share/My Music/Song.mp3"
            string absolutePath = Uri.UnescapeDataString(targetUri.AbsolutePath);

            // 2. 移除 ShareName 部分
            // 确保移除开头的 /
            string cleanPath = absolutePath.TrimStart('/');

            // 找到 ShareName 后的第一个斜杠
            int slashIndex = cleanPath.IndexOf('/');

            if (slashIndex == -1)
            {
                // 如果没有斜杠，说明就是 Share 根目录
                return string.Empty;
            }

            // 截取 Share 之后的部分
            string relativePath = cleanPath.Substring(slashIndex + 1);

            // 3. 转换为 Windows 风格的反斜杠 (SMB 协议要求)
            return relativePath.Replace("/", "\\");
        }
    }
}