using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace BetterLyrics.WinUI3.Models.FileSystem
{
    public interface IUnifiedFileSystem : IDisposable
    {
        Task<bool> ConnectAsync();
        Task<List<UnifiedFileItem>> GetFilesAsync(string relativePath);
        Task<Stream> OpenReadAsync(string fullPath);
        Task DisconnectAsync();
    }
}
