using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.FileSystemService.Providers;
using BetterLyrics.WinUI3.Services.LocalizationService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Logging;
using SQLite;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services.FileSystemService
{
    public partial class FileSystemService : BaseViewModel, IFileSystemService,
        IRecipient<PropertyChangedMessage<AutoScanInterval>>,
        IRecipient<PropertyChangedMessage<bool>>
    {
        private readonly ISettingsService _settingsService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger<FileSystemService> _logger;

        private readonly SQLiteAsyncConnection _db;
        private bool _isInitialized = false;

        // 定时器字典
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _folderTimerTokens = new();
        // 当前正在执行的扫描任务字典
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeScanTokens = new();

        private static readonly SemaphoreSlim _dbLock = new(1, 1);
        private static readonly SemaphoreSlim _folderScanLock = new(1, 1);

        public FileSystemService(ISettingsService settingsService, ILocalizationService localizationService, ILogger<FileSystemService> logger)
        {
            _logger = logger;
            _localizationService = localizationService;
            _settingsService = settingsService;
            _db = new SQLiteAsyncConnection(PathHelper.FilesCachePath);
        }

        /// <summary>
        /// 初始化（连接）数据库
        /// </summary>
        /// <returns></returns>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            await _db.CreateTableAsync<FileCacheEntity>();

            _isInitialized = true;
        }

        public async Task<List<FileCacheEntity>> GetFilesAsync(IUnifiedFileSystem provider, FileCacheEntity? parentFolder, string configId, bool forceRefresh = false)
        {
            await InitializeAsync();

            string queryParentUri;
            if (parentFolder == null)
            {
                if (!forceRefresh) forceRefresh = true;
                queryParentUri = "";
            }
            else
            {
                queryParentUri = parentFolder.Uri;
            }

            List<FileCacheEntity> cachedEntities = new List<FileCacheEntity>();

            if (parentFolder != null)
            {
                cachedEntities = await _db.Table<FileCacheEntity>()
                    .Where(x => x.MediaFolderId == configId && x.ParentUri == queryParentUri)
                    .ToListAsync();
            }

            bool needSync = forceRefresh || cachedEntities.Count == 0;

            if (needSync)
            {
                cachedEntities = await SyncAsync(provider, parentFolder, configId);
            }

            return cachedEntities;
        }

        /// <summary>
        /// 从远端/本地同步文件至数据库，该阶段不会解析文件全部元数据。
        /// <para/>
        /// 如果某个已有文件被修改或有新文件被添加，会预留空位，等待后续填充（通常交给 <see cref="ScanMediaFolderAsync"/> 完成）
        /// </summary>
        /// <param name="provider"></param>
        /// <param name="parentFolder"></param>
        /// <param name="configId"></param>
        /// <returns></returns>
        private async Task<List<FileCacheEntity>> SyncAsync(IUnifiedFileSystem provider, FileCacheEntity? parentFolder, string configId)
        {
            List<FileCacheEntity> remoteItems;
            try
            {
                remoteItems = await provider.GetFilesAsync(parentFolder);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Network sync error: {ex.Message}");
                return [];
            }

            if (remoteItems == null) return [];

            string targetParentUri = "";
            if (remoteItems.Count > 0)
            {
                targetParentUri = remoteItems[0].ParentUri ?? "";
            }
            else if (parentFolder != null)
            {
                targetParentUri = parentFolder.Uri;
            }
            else
            {
                return [];
            }

            try
            {
                await _db.RunInTransactionAsync(conn =>
                {
                    var dbItems = conn.Table<FileCacheEntity>()
                                      .Where(x => x.MediaFolderId == configId && x.ParentUri == targetParentUri)
                                      .ToList();

                    var dbMap = dbItems.ToDictionary(x => x.Uri, x => x);

                    var remoteMap = remoteItems.GroupBy(x => x.Uri)
                                               .Select(g => g.First())
                                               .ToDictionary(x => x.Uri, x => x);

                    var toInsert = new List<FileCacheEntity>();
                    var toUpdate = new List<FileCacheEntity>();
                    var toDelete = new List<FileCacheEntity>();

                    foreach (var remote in remoteItems)
                    {
                        if (dbMap.TryGetValue(remote.Uri, out var existing))
                        {
                            bool isChanged = existing.FileSize != remote.FileSize ||
                                             existing.LastModified != remote.LastModified;

                            if (isChanged)
                            {
                                existing.FileSize = remote.FileSize;
                                existing.LastModified = remote.LastModified;
                                existing.IsMetadataParsed = false; // 标记为未解析，下次会重新读取元数据

                                toUpdate.Add(existing);
                            }
                            else
                            {
                                // 数据库里原有的 Title, Artist, LocalAlbumArtPath 都会被完美保留
                            }
                        }
                        else
                        {
                            toInsert.Add(remote);
                        }
                    }

                    foreach (var dbItem in dbItems)
                    {
                        if (!remoteMap.ContainsKey(dbItem.Uri))
                        {
                            toDelete.Add(dbItem);
                        }
                    }

                    if (toInsert.Count > 0) conn.InsertAll(toInsert);
                    if (toUpdate.Count > 0) conn.UpdateAll(toUpdate);
                    if (toDelete.Count > 0)
                    {
                        foreach (var item in toDelete) conn.Delete(item);
                    }
                });

                var finalItems = await _db.Table<FileCacheEntity>()
                    .Where(x => x.MediaFolderId == configId && x.ParentUri == targetParentUri)
                    .ToListAsync();

                FolderUpdated?.Invoke(this, targetParentUri);

                return finalItems;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database sync error: {ex.Message}");
                return [];
            }
        }

        public async Task UpdateMetadataAsync(FileCacheEntity entity)
        {
            // 现在的实体已经包含了完整信息，直接 Update 即可
            // 我们只需要确保 Where 子句用的是主键或者 Uri

            // 简化版 SQL，直接用 ORM 的 Update
            // 但因为 entity 对象可能包含一些不应该被覆盖的旧数据（如果多线程操作），
            // 手写 SQL 只更新 Metadata 字段更安全。

            string sql = @"
                UPDATE FileCache 
                SET 
                    Title = ?, Artists = ?, Album = ?, 
                    Year = ?, Bitrate = ?, SampleRate = ?, BitDepth = ?, 
                    Duration = ?, AudioFormatName = ?, AudioFormatShortName = ?, Encoder = ?,
                    EmbeddedLyrics = ?, LocalAlbumArtPath = ?, 
                    IsMetadataParsed = 1 
                WHERE Id = ?"; // 推荐用 Id (主键) 最快，如果没有 Id 则用 Uri

            await _db.ExecuteAsync(sql,
                entity.Title, entity.Artists, entity.Album,
                entity.Year, entity.Bitrate, entity.SampleRate, entity.BitDepth,
                entity.Duration, entity.AudioFormatName, entity.AudioFormatShortName, entity.Encoder,
                entity.EmbeddedLyrics, entity.LocalAlbumArtPath,
                entity.Id // WHERE Id = ?
            );
        }

        public async Task<Stream?> OpenFileAsync(IUnifiedFileSystem provider, FileCacheEntity entity)
        {
            // 直接传递实体给 Provider
            return await provider.OpenReadAsync(entity);
        }

        public async Task DeleteCacheForMediaFolderAsync(MediaFolder folder)
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                folder.CleaningUpStatusText = _localizationService.GetLocalizedString("FileSystemServicePrepareToClean");
                folder.IsCleaningUp = true;
            });

            if (_folderTimerTokens.TryRemove(folder.Id, out var timerCts))
            {
                timerCts.Cancel();
                timerCts.Dispose();
                _logger.LogInformation("DeleteCacheForMediaFolderAsync: {}", "cts.Dispose();");
            }

            if (_activeScanTokens.TryGetValue(folder.Id, out var activeScanCts))
            {
                activeScanCts.Cancel();
                // 强制终止正在扫描的操作
            }

            try
            {
                await _folderScanLock.WaitAsync();

                try
                {
                    _dispatcherQueue.TryEnqueue(() =>
                    {
                        folder.CleaningUpStatusText = _localizationService.GetLocalizedString("FileSystemServiceCleaningCache");
                    });

                    await InitializeAsync();

                    await _dbLock.WaitAsync();
                    try
                    {
                        await _db.ExecuteAsync("DELETE FROM FileCache WHERE MediaFolderId = ?", folder.Id);
                        await _db.ExecuteAsync("VACUUM");
                    }
                    finally
                    {
                        _dbLock.Release();
                    }
                }
                finally
                {
                    _folderScanLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("DeleteCacheForMediaFolderAsync: {}", ex.Message);
            }
            finally
            {
                _dispatcherQueue.TryEnqueue(() =>
                {
                    folder.CleaningUpStatusText = "";
                    folder.IsCleaningUp = false;
                    folder.LastSyncTime = null;
                });
            }
        }

        public async Task ScanMediaFolderAsync(MediaFolder folder, CancellationToken token = default)
        {
            if (folder == null || !folder.IsEnabled) return;

            using var scanCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _activeScanTokens[folder.Id] = scanCts;

            _dispatcherQueue.TryEnqueue(() =>
            {
                folder.IsIndexing = true;
                folder.IndexingProgress = 0;
                folder.IndexingStatusText = _localizationService.GetLocalizedString("FileSystemServiceWaitingForScan");
            });

            try
            {
                await _folderScanLock.WaitAsync(scanCts.Token);

                _dispatcherQueue.TryEnqueue(() => folder.IndexingStatusText = _localizationService.GetLocalizedString("FileSystemServiceConnecting"));

                await InitializeAsync();

                using var fs = folder.CreateFileSystem();
                if (fs == null || !await fs.ConnectAsync())
                {
                    _dispatcherQueue.TryEnqueue(() => folder.IndexingStatusText = _localizationService.GetLocalizedString("FileSystemServiceConnectFailed"));
                    return;
                }

                _dispatcherQueue.TryEnqueue(() => folder.IndexingStatusText = _localizationService.GetLocalizedString("FileSystemServiceFetchingFileList"));

                var filesToProcess = new List<FileCacheEntity>();
                var foldersToScan = new Queue<FileCacheEntity?>();
                foldersToScan.Enqueue(null); // 根目录

                while (foldersToScan.Count > 0)
                {
                    if (scanCts.Token.IsCancellationRequested) return;

                    var currentParent = foldersToScan.Dequeue();

                    var items = await GetFilesAsync(fs, currentParent, folder.Id, forceRefresh: true);

                    foreach (var item in items)
                    {
                        if (item.IsDirectory)
                        {
                            foldersToScan.Enqueue(item);
                        }
                        else
                        {
                            var ext = Path.GetExtension(item.FileName).ToLower();
                            if (FileHelper.AllSupportedExtensions.Contains(ext))
                            {
                                filesToProcess.Add(item);
                            }
                        }
                    }
                }

                int total = filesToProcess.Count;
                int current = 0;

                foreach (var item in filesToProcess)
                {
                    if (scanCts.Token.IsCancellationRequested) return;

                    current++;

                    if (current % 10 == 0 || current == total)
                    {
                        double progress = (double)current / total * 100;
                        _dispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
                        {
                            folder.IndexingProgress = progress;
                            folder.IndexingStatusText = $"{_localizationService.GetLocalizedString("FileSystemServiceParsing")} {current}/{total}";
                        });
                    }

                    if (item.IsMetadataParsed) continue;

                    var ext = Path.GetExtension(item.FileName).ToLower();

                    try
                    {
                        if (FileHelper.MusicExtensions.Contains(ext))
                        {
                            using var originalStream = await OpenFileAsync(fs, item);
                            if (originalStream == null) continue;

                            ExtendedTrack track;
                            if (originalStream.CanSeek)
                            {
                                track = new ExtendedTrack(item, originalStream);
                            }
                            else
                            {
                                using var memStream = new MemoryStream();
                                await originalStream.CopyToAsync(memStream, scanCts.Token);
                                memStream.Position = 0;
                                track = new ExtendedTrack(item, memStream);
                            }

                            if (track.Duration > 0)
                            {
                                // 保存封面
                                string? artPath = await SaveAlbumArtToDiskAsync(track);

                                // 填充实体
                                item.Title = track.Title;
                                item.Artists = track.Artist;
                                item.Album = track.Album;
                                item.Year = track.Year;
                                item.Bitrate = track.Bitrate;
                                item.SampleRate = track.SampleRate;
                                item.BitDepth = track.BitDepth;
                                item.Duration = track.Duration;
                                item.AudioFormatName = track.AudioFormatName;
                                item.AudioFormatShortName = track.AudioFormatShortName;
                                item.Encoder = track.Encoder;
                                item.EmbeddedLyrics = track.RawLyrics; // 内嵌歌词
                                item.LocalAlbumArtPath = artPath;
                                item.IsMetadataParsed = true;
                            }
                        }
                        else if (FileHelper.LyricExtensions.Contains(ext))
                        {
                            using var stream = await OpenFileAsync(fs, item);
                            if (stream != null)
                            {
                                using var reader = new StreamReader(stream);
                                string content = await reader.ReadToEndAsync();

                                item.EmbeddedLyrics = content;
                                item.IsMetadataParsed = true;
                            }
                        }

                        if (item.IsMetadataParsed)
                        {
                            await _dbLock.WaitAsync(token);
                            try
                            {
                                await UpdateMetadataAsync(item);
                            }
                            finally
                            {
                                _dbLock.Release();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError("ScanMediaFolderAsync: {}", ex.Message);
                    }
                }

                _dispatcherQueue.TryEnqueue(() =>
                {
                    folder.LastSyncTime = DateTime.Now;
                });
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (Exception ex)
            {
                _dispatcherQueue.TryEnqueue(() => folder.IndexingStatusText = ex.Message);
            }
            finally
            {
                _folderScanLock.Release();

                _activeScanTokens.TryRemove(folder.Id, out _);

                _dispatcherQueue.TryEnqueue(() =>
                {
                    folder.IsIndexing = false;
                    folder.IndexingStatusText = "";
                    folder.IndexingProgress = 100;
                });
            }
        }

        public async Task<List<FileCacheEntity>> GetParsedFilesAsync(IEnumerable<string> enabledConfigIds)
        {
            await InitializeAsync();

            if (enabledConfigIds == null || !enabledConfigIds.Any())
            {
                return new List<FileCacheEntity>();
            }

            var idList = enabledConfigIds.ToList();

            // SQL 逻辑: SELECT * FROM FileCache WHERE IsMetadataParsed = 1 AND MediaFolderId IN (...)
            var results = await _db.Table<FileCacheEntity>()
                .Where(x => x.IsMetadataParsed && idList.Contains(x.MediaFolderId))
                .ToListAsync();

            return results;
        }

        public void StartAllFolderTimers()
        {
            foreach (var folder in _settingsService.AppSettings.LocalMediaFolders)
            {
                if (folder.IsEnabled)
                {
                    UpdateFolderTimer(folder);
                }
            }
        }

        private void UpdateFolderTimer(MediaFolder folder)
        {
            if (_folderTimerTokens.TryRemove(folder.Id, out var oldCts))
            {
                oldCts.Cancel();
                oldCts.Dispose();
            }

            if (!folder.IsEnabled || folder.ScanInterval == AutoScanInterval.Disabled)
            {
                return;
            }

            var newCts = new CancellationTokenSource();
            _folderTimerTokens[folder.Id] = newCts;

            TimeSpan period = folder.ScanInterval switch
            {
                AutoScanInterval.Every15Minutes => TimeSpan.FromMinutes(15),
                AutoScanInterval.EveryHour => TimeSpan.FromHours(1),
                AutoScanInterval.Every6Hours => TimeSpan.FromHours(6),
                AutoScanInterval.Daily => TimeSpan.FromDays(1),
                _ => TimeSpan.FromHours(1)
            };

            Task.Run(async () =>
            {
                try
                {
                    using var timer = new PeriodicTimer(period);

                    while (await timer.WaitForNextTickAsync(newCts.Token))
                    {
                        await ScanMediaFolderAsync(folder, newCts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"文件夹 {folder.Name} 定时扫描出错: {ex.Message}");
                }
            }, newCts.Token);
        }

        // 参数为 string parentUri，表示哪个文件夹的内容变了
        public event EventHandler<string>? FolderUpdated;

        private async Task<string?> SaveAlbumArtToDiskAsync(ExtendedTrack track)
        {
            var picData = track.AlbumArtByteArray;
            if (picData == null || picData.Length == 0) return null;

            try
            {
                string hash = ComputeHashForBytes(picData);
                string safeName = hash + ".jpg";

                string localPath = Path.Combine(PathHelper.LocalAlbumArtCacheDirectory, safeName);

                if (File.Exists(localPath)) return localPath;

                await File.WriteAllBytesAsync(localPath, picData);

                return localPath;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private string ComputeHashForBytes(byte[] data)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hashBytes = md5.ComputeHash(data);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            }
        }

        public void Receive(PropertyChangedMessage<AutoScanInterval> message)
        {
            if (message.Sender is MediaFolder mediaFolder)
            {
                if (message.PropertyName == nameof(MediaFolder.ScanInterval))
                {
                    UpdateFolderTimer(mediaFolder);
                }
            }
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is MediaFolder mediaFolder)
            {
                if (message.PropertyName == nameof(MediaFolder.IsEnabled))
                {
                    UpdateFolderTimer(mediaFolder);
                }
            }
        }

    }
}