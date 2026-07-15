using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Models.Settings;
using SMBLibrary;
using SMBLibrary.Client;
using FileAttributes = SMBLibrary.FileAttributes;

namespace BetterLyrics.Core.Implementations.Services.FileSystemService.Providers;

public class SMBFileSystem : IUnifiedFileSystem
{
    // 保存配置对象的引用
    private readonly MediaFolder _config;

    // 缓存解析出来的 Share 名称，因为 TreeConnect 要用
    private readonly string _shareName;
    private SMB2Client? _client;
    private ISMBFileStore? _fileStore;

    public SMBFileSystem(MediaFolder config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        // 在构造时就解析好 Share 名称，避免后续重复解析
        var uri = _config.GetStandardUri();

        // Segments[0] 是 "/", Segments[1] 是 "ShareName/"
        if (uri.Segments.Length > 1)
            _shareName = uri.Segments[1].TrimEnd('/');
        else
            // 如果没有 ShareName，这在 SMB 中通常是不合法的，但在根目录下可能发生
            _shareName = "";
    }

    public async Task<bool> ConnectAsync()
    {
        _client = new SMB2Client();

        // 连接主机
        var connected = _client.Connect(_config.UriHost, SMBTransportType.DirectTCPTransport);
        if (!connected) return false;

        // 登录
        var status = _client.Login(string.Empty, _config.UserName, _config.Password);
        if (status != NTStatus.STATUS_SUCCESS) return false;

        // 连接共享目录 (TreeConnect)
        // SMBLibrary 必须先连接到 Share，后续所有文件操作都是基于这个 Share 的相对路径
        if (string.IsNullOrEmpty(_shareName)) return false;

        _fileStore = _client.TreeConnect(_shareName, out status);
        return status == NTStatus.STATUS_SUCCESS;
    }

    /// <summary>
    ///     获取文件列表
    /// </summary>
    /// <param name="parentFolder">
    ///     传入要列出的文件夹实体。
    ///     如果传入 null，则默认列出 MediaFolder 配置的根目录。
    /// </param>
    public async Task<List<FilesIndexItem>> GetFilesAsync(FilesIndexItem? parentFolder = null)
    {
        var result = new List<FilesIndexItem>();
        if (_fileStore == null) return result;

        var smbPath = GetPathRelativeToShare(parentFolder);

        var statusRet = _fileStore.CreateFile(out var handle, out var status, smbPath,
            AccessMask.GENERIC_READ, FileAttributes.Directory, ShareAccess.Read,
            CreateDisposition.FILE_OPEN, CreateOptions.FILE_DIRECTORY_FILE, null);

        if (statusRet != NTStatus.STATUS_SUCCESS) return result;

        var parentUriString = parentFolder?.Uri ?? _config.GetStandardUri().AbsoluteUri;

        List<QueryDirectoryFileInformation> fileInfo;

        do
        {
            statusRet = _fileStore.QueryDirectory(out fileInfo, handle, "*",
                FileInformationClass.FileDirectoryInformation);

            // 如果查询失败或者没有更多文件，fileInfo 可能是 null，直接跳出
            if (statusRet != NTStatus.STATUS_SUCCESS && statusRet != NTStatus.STATUS_NO_MORE_FILES) break;

            // 如果是 NO_MORE_FILES 但 fileInfo 依然有残留数据（极少见），或者是 SUCCESS
            if (fileInfo != null)
                foreach (var item in fileInfo.Cast<FileDirectoryInformation>())
                {
                    if (item.FileName == "." || item.FileName == "..") continue;

                    // 过滤隐藏文件和系统文件
                    if ((item.FileAttributes & FileAttributes.Hidden) == FileAttributes.Hidden ||
                        (item.FileAttributes & FileAttributes.System) == FileAttributes.System)
                        continue;

                    var isDir = (item.FileAttributes & FileAttributes.Directory) == FileAttributes.Directory;

                    // 后缀名过滤
                    if (!isDir)
                    {
                        var extension = Path.GetExtension(item.FileName).ToLower();
                        if (string.IsNullOrEmpty(extension) || !FileHelper.AllSupportedExtensions.Contains(extension))
                            continue;
                    }

                    if (!parentUriString.EndsWith("/")) parentUriString += "/";
                    var baseUri = new Uri(parentUriString);
                    var newUri = new Uri(baseUri, item.FileName);

                    result.Add(new FilesIndexItem
                    {
                        MediaFolderId = _config.Id,
                        ParentUri = parentFolder?.Uri ?? _config.GetStandardUri().AbsoluteUri,

                        Uri = newUri.AbsoluteUri,

                        FileName = item.FileName,
                        IsDirectory = (item.FileAttributes & FileAttributes.Directory) == FileAttributes.Directory,
                        FileSize = item.AllocationSize,
                        LastModified = item.ChangeTime,
                        DateCreated = item.CreationTime
                    });
                }

            if (statusRet == NTStatus.STATUS_NO_MORE_FILES) break;
        } while (statusRet == NTStatus.STATUS_SUCCESS);

        _fileStore.CloseFile(handle);
        return result;
    }

    /// <summary>
    ///     打开文件流
    /// </summary>
    /// <param name="file">只需要传入文件实体即可</param>
    public async Task<Stream?> OpenReadAsync(FilesIndexItem file)
    {
        if (_fileStore == null || file == null) return null;

        var smbPath = GetPathRelativeToShare(file);

        var ret = _fileStore.CreateFile(out var handle, out var status, smbPath,
            AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE, 0, ShareAccess.Read, CreateDisposition.FILE_OPEN, 0,
            null);

        if (ret != NTStatus.STATUS_SUCCESS)
            throw new IOException($"SMB Open Error: {ret}");

        // SMBReadOnlyStream 内部已经实现了 2MB 的智能滑动缓存池，直接返回即可
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

    private string GetPathRelativeToShare(FilesIndexItem? entity)
    {
        Uri targetUri;

        if (entity == null)
            targetUri = _config.GetStandardUri();
        else
            targetUri = new Uri(entity.Uri);

        var absolutePath = Uri.UnescapeDataString(targetUri.AbsolutePath);
        var cleanPath = absolutePath.TrimStart('/');
        var slashIndex = cleanPath.IndexOf('/');

        if (slashIndex == -1) return string.Empty;

        var relativePath = cleanPath.Substring(slashIndex + 1);

        return relativePath.Replace("/", "\\");
    }
}