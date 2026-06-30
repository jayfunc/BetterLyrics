using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.DbContext;
using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LyricsMetadataParser = BetterLyrics.Core.Helpers.Lyrics.MetadataParser.LyricsMetadataParser;

namespace BetterLyrics.WinUI3.Services
{
    public partial class FileSystemService : BaseViewModel, IFileSystemService,
        IRecipient<PropertyChangedMessage<AutoScanInterval>>,
        IRecipient<PropertyChangedMessage<bool>>
    {
        private readonly ISettingsService _settingsService;
        private readonly ILocalizationService _localizationService;
        private readonly ILogger<FileSystemService> _logger;

        private readonly IDbContextFactory<FilesIndexDbContext> _contextFactory;

        // 定时器字典
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _folderTimerTokens = new();
        // 当前正在执行的扫描任务字典
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeScanTokens = new();

        private static readonly SemaphoreSlim _folderScanLock = new(1, 1);

        public FileSystemService(
            ISettingsService settingsService,
            ILocalizationService localizationService,
            ILogger<FileSystemService> logger,
            IDbContextFactory<FilesIndexDbContext> contextFactory)
        {
            _logger = logger;
            _localizationService = localizationService;
            _settingsService = settingsService;
            _contextFactory = contextFactory;
        }

        public async Task<List<FilesIndexItem>> GetFilesAsync(IUnifiedFileSystem provider, FilesIndexItem? parentFolder, string configId, bool forceSync = false)
        {
            string queryParentUri = parentFolder == null ? "" : parentFolder.Uri;

            using var context = await _contextFactory.CreateDbContextAsync();

            var cachedEntities = await context.FilesIndex
                .AsNoTracking() // 读操作不追踪，提升性能
                .Where(x => x.MediaFolderId == configId && x.ParentUri == queryParentUri)
                .ToListAsync();

            // SyncAsync 内部自己管理 Context
            cachedEntities = await SyncAsync(provider, parentFolder, configId, forceSync);

            return cachedEntities;
        }

        /// <summary>
        /// 从远端/本地同步文件至数据库
        /// </summary>
        private async Task<List<FilesIndexItem>> SyncAsync(IUnifiedFileSystem provider, FilesIndexItem? parentFolder, string configId, bool forceSync = false)
        {
            List<FilesIndexItem> remoteItems;
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
                targetParentUri = remoteItems[0].ParentUri ?? "";
            else if (parentFolder != null)
                targetParentUri = parentFolder.Uri;
            else
                return [];

            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // 开启事务 (EF Core 也能管理事务)
                using var transaction = await context.Database.BeginTransactionAsync();

                // 1. 获取数据库中现有的该目录下的文件
                var dbItems = await context.FilesIndex
                    .Where(x => x.MediaFolderId == configId && x.ParentUri == targetParentUri)
                    .ToListAsync();

                var dbMap = dbItems.ToDictionary(x => x.Uri, x => x);

                // 2. 远端数据去重（防止 Provider 返回重复 Uri）
                var remoteDistinct = remoteItems
                    .GroupBy(x => x.Uri)
                    .Select(g => g.First())
                    .ToList();

                var remoteUris = new HashSet<string>();

                // 3. 处理 新增 和 更新
                foreach (var remote in remoteDistinct)
                {
                    remoteUris.Add(remote.Uri);

                    if (dbMap.TryGetValue(remote.Uri, out var existing))
                    {
                        // 检查是否变更
                        bool isChanged = existing.FileSize != remote.FileSize ||
                            existing.LastModified != remote.LastModified ||
                            forceSync;

                        if (isChanged)
                        {
                            existing.FileSize = remote.FileSize;
                            existing.LastModified = remote.LastModified;
                            existing.IsMetadataParsed = false; // 标记重新解析

                            // EF Core 自动追踪 existing 的变化，无需手动 Update
                        }
                    }
                    else
                    {
                        // 新增
                        // 注意：如果 Id 是自增的，不要手动赋值 Id，除非是 Guid
                        context.FilesIndex.Add(remote);
                    }
                }

                // 4. 处理 删除 (数据库有，远端没有)
                foreach (var dbItem in dbItems)
                {
                    if (!remoteUris.Contains(dbItem.Uri))
                    {
                        context.FilesIndex.Remove(dbItem);
                    }
                }

                await context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 5. 返回最新数据
                // 这里的 dbItems 已经被 Update 更新了内存状态，但 Remove 的还在列表里，Add 的不在列表里
                // 所以最稳妥的是重新查一次，或者手动维护列表。为了准确性，重新查询 (AsNoTracking)
                var finalItems = await context.FilesIndex
                    .AsNoTracking()
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

        public async Task UpdateMetadataAsync(FilesIndexItem entity)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            // 使用 EF Core 7.0+ 的 ExecuteUpdateAsync 高效更新
            // 这会直接生成 UPDATE SQL，不经过内存加载，性能极高
            await context.FilesIndex
                .Where(x => x.Id == entity.Id) // 优先用 Id
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Title, entity.Title)
                    .SetProperty(p => p.Artist, entity.Artist)
                    .SetProperty(p => p.Album, entity.Album)
                    .SetProperty(p => p.Year, entity.Year)
                    .SetProperty(p => p.Bitrate, entity.Bitrate)
                    .SetProperty(p => p.SampleRate, entity.SampleRate)
                    .SetProperty(p => p.BitDepth, entity.BitDepth)
                    .SetProperty(p => p.Duration, entity.Duration)
                    .SetProperty(p => p.AudioFormatName, entity.AudioFormatName)
                    .SetProperty(p => p.AudioFormatShortName, entity.AudioFormatShortName)
                    .SetProperty(p => p.Encoder, entity.Encoder)
                    .SetProperty(p => p.EmbeddedLyrics, entity.EmbeddedLyrics)
                    .SetProperty(p => p.LocalAlbumArtPath, entity.LocalAlbumArtPath)
                    .SetProperty(p => p.IsMetadataParsed, true)
                );
        }

        public async Task<Stream?> OpenFileAsync(IUnifiedFileSystem provider, FilesIndexItem entity)
        {
            return await provider.OpenReadAsync(entity);
        }

        public async Task DeleteCacheForMediaFolderAsync(MediaFolder folder)
        {
            AppUIThread.Execute(() =>
            {
                folder.IndexingProgress = 0;
                folder.StatusSeverity = MessageSeverity.Informational;
                folder.StatusText = _localizationService.GetLocalizedString("FileSystemServicePrepareToClean");
                folder.IsProcessing = true;
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
            }

            try
            {
                await _folderScanLock.WaitAsync();

                try
                {
                    AppUIThread.Execute(() =>
                    {
                        folder.StatusText = _localizationService.GetLocalizedString("FileSystemServiceCleaningCache");
                    });

                    using var context = await _contextFactory.CreateDbContextAsync();

                    await context.FilesIndex
                        .Where(x => x.MediaFolderId == folder.Id)
                        .ExecuteDeleteAsync();

                    await context.Database.ExecuteSqlRawAsync("VACUUM");
                }
                finally
                {
                    _folderScanLock.Release();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DeleteCacheForMediaFolderAsync");
            }
            finally
            {
                AppUIThread.Execute(() =>
                {
                    folder.IsProcessing = false;
                    folder.LastSyncTime = null;
                });
            }
        }

        public async Task ScanMediaFolderAsync(MediaFolder folder, bool forceSync = false, CancellationToken token = default)
        {
            if (folder == null || !folder.IsEnabled) return;

            using var scanCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            _activeScanTokens[folder.Id] = scanCts;

            AppUIThread.Execute(() =>
            {
                folder.StatusSeverity = MessageSeverity.Informational;
                folder.IsProcessing = true;
                folder.IndexingProgress = 0;
                folder.StatusText = _localizationService.GetLocalizedString("FileSystemServiceWaitingForScan");
            });

            try
            {
                await _folderScanLock.WaitAsync(scanCts.Token);

                AppUIThread.Execute(() => folder.StatusText = _localizationService.GetLocalizedString("FileSystemServiceConnecting"));

                using var fs = folder.CreateFileSystem();
                if (fs == null || !await fs.ConnectAsync())
                {
                    AppUIThread.Execute(() =>
                    {
                        folder.StatusSeverity = MessageSeverity.Error;
                        folder.StatusText = _localizationService.GetLocalizedString("FileSystemServiceConnectFailed");
                    });
                    return;
                }

                AppUIThread.Execute(() => folder.StatusText = _localizationService.GetLocalizedString("FileSystemServiceFetchingFileList"));

                var filesToProcess = new List<FilesIndexItem>();
                var foldersToScan = new Queue<FilesIndexItem?>();
                foldersToScan.Enqueue(null); // 根目录

                while (foldersToScan.Count > 0)
                {
                    if (scanCts.Token.IsCancellationRequested) return;

                    var currentParent = foldersToScan.Dequeue();
                    var items = await GetFilesAsync(fs, currentParent, folder.Id, forceSync);

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
                        AppUIThread.Execute(() =>
                        {
                            folder.IndexingProgress = progress;
                            folder.StatusText = $"{_localizationService.GetLocalizedString("FileSystemServiceParsing")} {current}/{total}";
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
                                string? artPath = await SaveAlbumArtToDiskAsync(track);

                                item.Title = track.Title;
                                item.Artist = track.Artist;
                                item.Album = track.Album;
                                item.Year = track.Year;
                                item.Bitrate = track.Bitrate;
                                item.SampleRate = track.SampleRate;
                                item.BitDepth = track.BitDepth;
                                item.Duration = track.Duration;
                                item.AudioFormatName = track.AudioFormatName;
                                item.AudioFormatShortName = track.AudioFormatShortName;
                                item.Encoder = track.Encoder;
                                item.EmbeddedLyrics = track.RawLyrics;
                                item.LocalAlbumArtPath = artPath;
                                item.IsMetadataParsed = true;
                            }
                        }
                        else if (FileHelper.LyricExtensions.Contains(ext))
                        {
                            using var stream = await OpenFileAsync(fs, item);
                            if (stream != null)
                            {
                                using (var reader = new StreamReader(stream))
                                {
                                    string content = await reader.ReadToEndAsync(token);
                                    item.EmbeddedLyrics = content;

                                    var metadata = LyricsMetadataParser.Parse(content, ext);
                                    item.Title = metadata.Title;
                                    item.Artist = metadata.Artist;
                                    item.Album = metadata.Album;
                                    item.Duration = (int)metadata.TotalSeconds;
                                }
                                item.IsMetadataParsed = true;
                            }
                        }

                        if (item.IsMetadataParsed)
                        {
                            // 更新操作：直接调用 UpdateMetadataAsync
                            // 此时不需要 _dbLock，因为 UpdateMetadataAsync 内部会 CreateDbContextAsync
                            // 而 _folderScanLock 已经保证了当前文件夹扫描的独占性
                            await UpdateMetadataAsync(item);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ScanMediaFolderAsync");
                    }
                }

                AppUIThread.Execute(() =>
                {
                    folder.StatusSeverity = MessageSeverity.Success;
                    folder.StatusText = _localizationService.GetLocalizedString("FileSystemServiceReady");
                    folder.LastSyncTime = DateTime.Now;
                });
            }
            catch (OperationCanceledException)
            {
                // 正常取消
            }
            catch (Exception ex)
            {
                AppUIThread.Execute(() =>
                {
                    folder.StatusText = ex.Message;
                    folder.StatusSeverity = MessageSeverity.Error;
                });
            }
            finally
            {
                _folderScanLock.Release();
                _activeScanTokens.TryRemove(folder.Id, out _);

                AppUIThread.Execute(() =>
                {
                    folder.IsProcessing = false;
                    folder.IndexingProgress = 0;
                });
            }
        }

        public async Task<List<FilesIndexItem>> GetParsedFilesAsync()
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            // SQL: SELECT * FROM FileCache WHERE IsMetadataParsed = 1 AND MediaFolderId IN (...)
            return await context.FilesIndex
                .AsNoTracking()
                .Where(x => x.IsMetadataParsed)
                .ToListAsync();
        }

        public async Task<List<FilesIndexItem>> GetParsedFilesAsync(IEnumerable<string> enabledConfigIds, CancellationToken token = default)
        {
            if (enabledConfigIds == null || !enabledConfigIds.Any())
            {
                return new List<FilesIndexItem>();
            }

            var idList = enabledConfigIds.ToList();

            using var context = await _contextFactory.CreateDbContextAsync(token);

            // SQL: SELECT * FROM FileCache WHERE IsMetadataParsed = 1 AND MediaFolderId IN (...)
            return await context.FilesIndex
                .AsNoTracking()
                .Where(x => x.IsMetadataParsed && idList.Contains(x.MediaFolderId))
                .ToListAsync(token);
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

            _ = Task.Run(async () =>
            {
                try
                {
                    using var timer = new PeriodicTimer(period);

                    while (await timer.WaitForNextTickAsync(newCts.Token))
                    {
                        await ScanMediaFolderAsync(folder, token: newCts.Token);
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

        public event EventHandler<string>? FolderUpdated;

        private async Task<string?> SaveAlbumArtToDiskAsync(ExtendedTrack track)
        {
            // 代码未变，纯 IO 操作
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