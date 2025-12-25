using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.FileSystemService
{
    public interface IFileSystemService
    {
        Task InitializeAsync();
        Task<List<UnifiedFileItem>> GetFilesAsync(string relativePath);
        Task<Stream> OpenFileAsync(UnifiedFileItem item);

        event EventHandler<string> FolderUpdated;
    }
}