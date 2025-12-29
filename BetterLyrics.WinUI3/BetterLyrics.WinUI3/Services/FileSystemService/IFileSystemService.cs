using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.FileSystemService.Providers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.FileSystemService
{
    public interface IFileSystemService
    {
        /// <summary>
        /// 初始化（连接）数据库
        /// </summary>
        /// <returns></returns>
        Task InitializeAsync();

        /// <summary>
        /// 从数据库拉取文件（必要时需要从远端/本地同步至数据库）
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="parentFolder"></param>
        /// <param name="configId"></param>
        /// <param name="forceRefresh">强制需要从远端/本地同步至数据库</param>
        /// <returns></returns>
        Task<List<FileCacheEntity>> GetFilesAsync(IUnifiedFileSystem provider, FileCacheEntity? parentFolder, string configId, bool forceRefresh = false);

        /// <summary>
        /// 打开文件（通过远端/本地流）
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task<Stream?> OpenFileAsync(IUnifiedFileSystem provider, FileCacheEntity entity);

        /// <summary>
        /// 更新数据库（单个文件）
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Task UpdateMetadataAsync(FileCacheEntity entity);

        /// <summary>
        /// 从数据库删除
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        Task DeleteCacheForMediaFolderAsync(MediaFolder folder);

        /// <summary>
        /// 从数据库拉取文件（必要时需要从远端/本地同步至数据库）。对于需要解析的文件，打开流填充元数据并回写至数据库。
        /// </summary>
        /// <param name="folder"></param>
        /// <returns></returns>
        Task ScanMediaFolderAsync(MediaFolder folder, CancellationToken token = default);

        /// <summary>
        /// 从数据库拉取
        /// </summary>
        /// <param name="enabledConfigIds"></param>
        /// <returns></returns>
        Task<List<FileCacheEntity>> GetParsedFilesAsync(IEnumerable<string> enabledConfigIds);

        void StartAllFolderTimers();

        event EventHandler<string> FolderUpdated;
    }
}