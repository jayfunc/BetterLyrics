using System.Net;
using System.Net.Http;
using System.IO;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Models.Settings;
using WebDav;

namespace BetterLyrics.Core.Implementations.Services.FileSystemService.Providers;

public class WebDavFileSystem : IUnifiedFileSystem
{
    private readonly Uri _baseAddress;
    private readonly WebDavClient _client;
    private readonly MediaFolder _config;
    private readonly HttpClient _httpClient;

    public WebDavFileSystem(MediaFolder config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        // 构建 BaseAddress (只包含 http://host:port/)
        // MediaFolder.GetStandardUri() 返回的是带路径的完整 URI (http://host:port/path)
        // 提取出根用于初始化 WebDavClient
        var fullUri = _config.GetStandardUri();

        // 提取 "http://host:port"
        _baseAddress = new Uri($"{fullUri.Scheme}://{fullUri.Authority}");

        _client = new WebDavClient(new WebDavClientParams
        {
            BaseAddress = _baseAddress,
            Credentials = new NetworkCredential(_config.UserName, _config.Password)
        });

        var handler = new HttpClientHandler
        {
            Credentials = new NetworkCredential(_config.UserName, _config.Password)
        };
        _httpClient = new HttpClient(handler);
    }

    public async Task<bool> ConnectAsync()
    {
        var result = await _client.Propfind(_config.GetStandardUri().AbsoluteUri);
        return result.IsSuccessful;
    }

    public async Task<List<FilesIndexItem>> GetFilesAsync(FilesIndexItem? parentFolder = null)
    {
        var list = new List<FilesIndexItem>();

        Uri targetUri;
        if (parentFolder == null)
            targetUri = _config.GetStandardUri();
        else
            targetUri = new Uri(parentFolder.Uri);

        var result = await _client.Propfind(targetUri.AbsoluteUri);

        if (result.IsSuccessful)
        {
            var parentUriString = targetUri.AbsoluteUri;
            if (!parentUriString.EndsWith("/")) parentUriString += "/";

            var targetPathClean = targetUri.AbsolutePath.TrimEnd('/');

            foreach (var res in result.Resources)
            {
                var itemUri = new Uri(_baseAddress, res.Uri);

                // 过滤掉文件夹自身
                if (itemUri.AbsolutePath.TrimEnd('/') == targetPathClean) continue;

                var name = res.DisplayName;
                if (string.IsNullOrEmpty(name))
                {
                    name = itemUri.AbsolutePath.TrimEnd('/').Split('/').Last();
                    name = WebUtility.UrlDecode(name);
                }

                if (string.IsNullOrEmpty(name)) continue;

                if (name.StartsWith(".")) continue;

                var isDir = res.IsCollection;
                if (!isDir)
                {
                    var extension = Path.GetExtension(name).ToLower();
                    // 如果后缀为空或不在白名单，跳过
                    if (string.IsNullOrEmpty(extension) ||
                        !FileHelper.AllSupportedExtensions.Contains(extension)) continue;
                }

                list.Add(new FilesIndexItem
                {
                    MediaFolderId = _config.Id,

                    ParentUri = parentFolder?.Uri ?? _config.GetStandardUri().AbsoluteUri,

                    Uri = itemUri.AbsoluteUri,

                    FileName = name,
                    IsDirectory = res.IsCollection,

                    FileSize = res.ContentLength ?? 0,
                    LastModified = res.LastModifiedDate ?? DateTime.MinValue,
                    DateCreated = res.CreationDate ?? (res.LastModifiedDate ?? DateTime.MinValue)
                });
            }
        }

        return list;
    }

    public async Task<Stream?> OpenReadAsync(FilesIndexItem entity)
    {
        if (entity == null) return null;

        // WebDavReadOnlyStream 内部已经实现了智能缓存池机制，直接返回即可
        return new WebDavReadOnlyStream(_httpClient, entity.Uri, entity.FileSize);
    }

    public async Task DisconnectAsync()
    {
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _client?.Dispose();
        _httpClient?.Dispose();
    }
}