using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Interfaces.Services;

public interface IFileSystemService
{
    /// <summary>
    ///     从数据库拉取文件（必要时需要从远端/本地同步至数据库）
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="parentFolder"></param>
    /// <param name="configId"></param>
    /// <param name="forceRefresh">强制需要从远端/本地同步至数据库</param>
    /// <returns></returns>
    Task<List<FilesIndexItem>> GetFilesAsync(IUnifiedFileSystem provider, FilesIndexItem? parentFolder, string configId,
        bool forceRefresh = false);

    /// <summary>
    ///     打开文件（通过远端/本地流）
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task<Stream?> OpenFileAsync(IUnifiedFileSystem provider, FilesIndexItem entity);

    /// <summary>
    ///     更新数据库（单个文件）
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task UpdateMetadataAsync(FilesIndexItem entity);

    /// <summary>
    ///     从数据库删除
    /// </summary>
    /// <param name="folder"></param>
    /// <returns></returns>
    Task DeleteCacheForMediaFolderAsync(MediaFolder folder);

    /// <summary>
    ///     从数据库拉取文件（必要时需要从远端/本地同步至数据库）。对于需要解析的文件，打开流填充元数据并回写至数据库。
    /// </summary>
    /// <param name="folder"></param>
    /// <returns></returns>
    Task ScanMediaFolderAsync(MediaFolder folder, bool forceSync = false, CancellationToken token = default);

    /// <summary>
    ///     从数据库拉取全部已解析的数据
    /// </summary>
    /// <returns></returns>
    Task<List<FilesIndexItem>> GetParsedFilesAsync();

    /// <summary>
    ///     从数据库拉取全部已解析的且其所属的 MediaFolder 在应用内处于开启状态的数据
    /// </summary>
    /// <param name="enabledConfigIds"></param>
    /// <exception cref="OperationCanceledException"></exception>
    /// <returns></returns>
    Task<List<FilesIndexItem>> GetParsedFilesAsync(IEnumerable<string> enabledConfigIds,
        CancellationToken token = default);

    void StartAllFolderTimers();
}