using BetterLyrics.Core.Models.Entities;

namespace BetterLyrics.Core.Interfaces.Services;

public interface IUnifiedFileSystem : IDisposable
{
    Task<bool> ConnectAsync();

    /// <summary>
    ///     从流拉取
    /// </summary>
    /// <param name="parentFolder"></param>
    /// <returns></returns>
    Task<List<FilesIndexItem>> GetFilesAsync(FilesIndexItem? parentFolder = null);

    /// <summary>
    ///     打开流
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    Task<Stream?> OpenReadAsync(FilesIndexItem file);

    Task DisconnectAsync();
}