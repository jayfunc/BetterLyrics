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
        Task<List<UnifiedFileItem>> GetFilesAsync(string relativePath);
        Task<Stream> OpenReadAsync(string fullPath);
        Task DisconnectAsync();
    }
}
