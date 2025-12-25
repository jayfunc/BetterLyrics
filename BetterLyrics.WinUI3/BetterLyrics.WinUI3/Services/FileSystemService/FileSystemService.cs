using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.FileSystemService.Providers;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.FileSystemService
{
    public class FileSystemService : IFileSystemService
    {
        private readonly IUnifiedFileSystem _provider;
        private readonly SQLiteAsyncConnection _db;
        private bool _isInitialized = false;

        public FileSystemService(IUnifiedFileSystem provider)
        {
            _provider = provider;

            var dbPath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "files_cache.db");
            _db = new SQLiteAsyncConnection(dbPath);
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await _provider.ConnectAsync();

            await _db.CreateTableAsync<FileCacheEntity>();
            _isInitialized = true;
        }

        public async Task<List<UnifiedFileItem>> GetFilesAsync(string relativePath)
        {
            await InitializeAsync();

            var cachedEntities = await _db.Table<FileCacheEntity>()
                .Where(x => x.ParentPath == relativePath)
                .ToListAsync();

            var result = cachedEntities.Select(x => new UnifiedFileItem
            {
                Name = x.Name,
                FullPath = x.FullPath,
                IsFolder = x.IsFolder,
            }).ToList();

            _ = SyncInBackground(relativePath);
            return result;
        }

        private async Task SyncInBackground(string relativePath)
        {
            var remoteItems = await _provider.GetFilesAsync(relativePath);

            var newEntities = remoteItems.Select(item => new FileCacheEntity
            {
                ParentPath = relativePath,

                FullPath = item.FullPath,
                Name = item.Name,
                IsFolder = item.IsFolder,
            });

            await _db.RunInTransactionAsync(conn =>
            {
                conn.Execute("DELETE FROM FileCache WHERE ParentPath = ?", relativePath);
                conn.InsertAll(newEntities);
            });

            FolderUpdated?.Invoke(this, relativePath);
        }

        public async Task<Stream> OpenFileAsync(UnifiedFileItem item)
        {
            return await _provider.OpenReadAsync(item.FullPath);
        }

        public event EventHandler<string>? FolderUpdated;

    }
}