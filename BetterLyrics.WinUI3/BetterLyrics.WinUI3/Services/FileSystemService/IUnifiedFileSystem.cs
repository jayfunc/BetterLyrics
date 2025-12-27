using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.FileSystemService
{
    public interface IUnifiedFileSystem : IDisposable
    {
        Task<bool> ConnectAsync();
        /// <summary>
        /// 从流拉取
        /// </summary>
        /// <param name="parentFolder"></param>
        /// <returns></returns>
        Task<List<FileCacheEntity>> GetFilesAsync(FileCacheEntity? parentFolder = null);
        /// <summary>
        /// 打开流
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        Task<Stream?> OpenReadAsync(FileCacheEntity file);
        Task DisconnectAsync();
    }
}
