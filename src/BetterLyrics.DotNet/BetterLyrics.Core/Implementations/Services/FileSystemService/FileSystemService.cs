using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Entities;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LiteDB;
using Microsoft.Extensions.Logging;
using LyricsMetadataParser = BetterLyrics.Core.Helpers.Lyrics.MetadataParser.LyricsMetadataParser;

namespace BetterLyrics.Core.Implementations.Services.FileSystemService;

public class FileSystemService : BaseViewModel, IFileSystemService,
    IRecipient<PropertyChangedMessage<AutoScanInterval>>,
    IRecipient<PropertyChangedMessage<bool>>
{
    private static readonly SemaphoreSlim _folderScanLock = new(1, 1);

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _activeScanTokens = new();
    private readonly IAppUIThreadProvider _appUIThreadProvider;

    private readonly IDatabaseService _databaseService;

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _folderTimerTokens = new();
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<FileSystemService> _logger;
    private readonly ISettingsService _settingsService;

    public FileSystemService(
        ISettingsService settingsService,
        ILocalizationService localizationService,
        ILogger<FileSystemService> logger,
        IDatabaseService databaseService, IAppUIThreadProvider appUiThreadProvider)
    {
        _logger = logger;
        _localizationService = localizationService;
        _settingsService = settingsService;
        _databaseService = databaseService;
        _appUIThreadProvider = appUiThreadProvider;

        var col = _databaseService.FilesIndexDb.GetCollection<FilesIndexItem>("filesIndex");
        col.EnsureIndex(x => x.MediaFolderId);
        col.EnsureIndex(x => x.ParentUri);
        col.EnsureIndex(x => x.Uri, true);
    }

    private ILiteCollection<FilesIndexItem> GetCollection()
    {
        return _databaseService.FilesIndexDb.GetCollection<FilesIndexItem>("filesIndex");
    }

    public async Task<List<FilesIndexItem>> GetFilesAsync(IUnifiedFileSystem provider, FilesIndexItem? parentFolder,
        string configId, bool forceSync = false)
    {
        var cachedEntities = await SyncAsync(provider, parentFolder, configId, forceSync);

        return cachedEntities;
    }

    public Task UpdateMetadataAsync(FilesIndexItem entity)
    {
        var col = GetCollection();
        // To be safe, we fetch the item by Id or Uri, and update the whole object
        var dbItem = col.FindById(entity.Id);
        if (dbItem != null)
        {
            dbItem.Title = entity.Title;
            dbItem.Artists = entity.Artists;
            dbItem.Album = entity.Album;
            dbItem.Year = entity.Year;
            dbItem.Bitrate = entity.Bitrate;
            dbItem.SampleRate = entity.SampleRate;
            dbItem.BitDepth = entity.BitDepth;
            dbItem.Duration = entity.Duration;
            dbItem.AudioFormatName = entity.AudioFormatName;
            dbItem.AudioFormatShortName = entity.AudioFormatShortName;
            dbItem.Encoder = entity.Encoder;
            dbItem.EmbeddedLyrics = entity.EmbeddedLyrics;
            dbItem.LocalAlbumArtPath = entity.LocalAlbumArtPath;
            dbItem.IsMetadataParsed = true;

            col.Update(dbItem);
        }

        return Task.CompletedTask;
    }

    public async Task<Stream?> OpenFileAsync(IUnifiedFileSystem provider, FilesIndexItem entity)
    {
        return await provider.OpenReadAsync(entity);
    }

    public async Task DeleteCacheForMediaFolderAsync(MediaFolder folder)
    {
        _appUIThreadProvider.Execute(() =>
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

        if (_activeScanTokens.TryGetValue(folder.Id, out var activeScanCts)) activeScanCts.Cancel();

        try
        {
            await _folderScanLock.WaitAsync();

            try
            {
                _appUIThreadProvider.Execute(() =>
                {
                    folder.StatusText =
                        _localizationService.GetLocalizedString("FileSystemServiceCleaningCache");
                });

                var col = GetCollection();
                col.DeleteMany(x => x.MediaFolderId == folder.Id);
                _databaseService.FilesIndexDb.Rebuild();
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
            _appUIThreadProvider.Execute(() =>
            {
                folder.IsProcessing = false;
                folder.LastSyncTime = null;
            });
        }
    }

    public async Task ScanMediaFolderAsync(MediaFolder folder, bool forceSync = false,
        CancellationToken token = default)
    {
        if (folder == null || !folder.IsEnabled) return;

        using var scanCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        _activeScanTokens[folder.Id] = scanCts;

        _appUIThreadProvider.Execute(() =>
        {
            folder.StatusSeverity = MessageSeverity.Informational;
            folder.IsProcessing = true;
            folder.IndexingProgress = 0;
            folder.StatusText = _localizationService.GetLocalizedString("FileSystemServiceWaitingForScan");
        });

        try
        {
            await _folderScanLock.WaitAsync(scanCts.Token);

            _appUIThreadProvider.Execute(() =>
                folder.StatusText = _localizationService.GetLocalizedString("FileSystemServiceConnecting"));

            using var fs = folder.CreateFileSystem();
            if (fs == null || !await fs.ConnectAsync())
            {
                _appUIThreadProvider.Execute(() =>
                {
                    folder.StatusSeverity = MessageSeverity.Error;
                    folder.StatusText = _localizationService.GetLocalizedString("FileSystemServiceConnectFailed");
                });
                return;
            }

            _appUIThreadProvider.Execute(() =>
                folder.StatusText = _localizationService.GetLocalizedString("FileSystemServiceFetchingFileList"));

            var filesToProcess = new List<FilesIndexItem>();
            var foldersToScan = new Queue<FilesIndexItem?>();
            foldersToScan.Enqueue(null); // 根目录

            while (foldersToScan.Count > 0)
            {
                if (scanCts.Token.IsCancellationRequested) return;

                var currentParent = foldersToScan.Dequeue();
                var items = await GetFilesAsync(fs, currentParent, folder.Id, forceSync);

                foreach (var item in items)
                    if (item.IsDirectory)
                    {
                        foldersToScan.Enqueue(item);
                    }
                    else
                    {
                        var ext = Path.GetExtension(item.FileName).ToLower();
                        if (FileHelper.AllSupportedExtensions.Contains(ext)) filesToProcess.Add(item);
                    }
            }

            var total = filesToProcess.Count;
            var current = 0;

            foreach (var item in filesToProcess)
            {
                if (scanCts.Token.IsCancellationRequested) return;

                current++;

                if (current % 10 == 0 || current == total)
                {
                    var progress = (double)current / total * 100;
                    _appUIThreadProvider.Execute(() =>
                    {
                        folder.IndexingProgress = progress;
                        folder.StatusText =
                            $"{_localizationService.GetLocalizedString("FileSystemServiceParsing")} {current}/{total}";
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
                            // 针对不支持 Seek 的流，仅读取前 3MB 以避免下载整个庞大的音频文件
                            using var memStream = new MemoryStream();
                            var buffer = new byte[81920];
                            long maxBytesToRead = 3 * 1024 * 1024; // 3MB
                            long totalRead = 0;
                            int bytesRead;

                            while (totalRead < maxBytesToRead && (bytesRead = await originalStream.ReadAsync(buffer, 0, (int)Math.Min(buffer.Length, maxBytesToRead - totalRead), scanCts.Token)) > 0)
                            {
                                await memStream.WriteAsync(buffer.AsMemory(0, bytesRead), scanCts.Token);
                                totalRead += bytesRead;
                            }

                            memStream.Position = 0;
                            track = new ExtendedTrack(item, memStream);
                        }

                        if (track.Duration > 0)
                        {
                            var artPath = await SaveAlbumArtToDiskAsync(track);

                            item.Title = track.Title;
                            item.Artists = track.Artist;
                            item.Album = track.Album;
                            item.Year = track.Year;
                            item.Genre = track.Genre;
                            item.TrackNumber = track.TrackNumber;
                            item.DiscNumber = track.DiscNumber;
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
                                var content = await reader.ReadToEndAsync(token);
                                item.EmbeddedLyrics = content;

                                var metadata = LyricsMetadataParser.Parse(content, ext);
                                item.Title = metadata.Title;
                                item.Artists = metadata.Artist;
                                item.Album = metadata.Album;
                                item.Duration = (int)metadata.TotalSeconds;
                            }

                            item.IsMetadataParsed = true;
                        }
                    }

                    if (item.IsMetadataParsed)
                        await UpdateMetadataAsync(item);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "ScanMediaFolderAsync");
                }
            }

            _appUIThreadProvider.Execute(() =>
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
            _appUIThreadProvider.Execute(() =>
            {
                folder.StatusText = ex.Message;
                folder.StatusSeverity = MessageSeverity.Error;
            });
        }
        finally
        {
            _folderScanLock.Release();
            _activeScanTokens.TryRemove(folder.Id, out _);

            _appUIThreadProvider.Execute(() =>
            {
                folder.IsProcessing = false;
                folder.IndexingProgress = 0;
            });
        }
    }

    public Task<List<FilesIndexItem>> GetParsedFilesAsync()
    {
        var col = GetCollection();
        var list = col.Find(x => x.IsMetadataParsed).ToList();
        return Task.FromResult(list);
    }

    public Task<List<FilesIndexItem>> GetParsedFilesAsync(IEnumerable<string> enabledConfigIds,
        CancellationToken token = default)
    {
        if (enabledConfigIds == null || !enabledConfigIds.Any()) return Task.FromResult(new List<FilesIndexItem>());

        var idList = enabledConfigIds.ToList();
        var col = GetCollection();

        var list = col.Find(x => x.IsMetadataParsed && idList.Contains(x.MediaFolderId)).ToList();

        return Task.FromResult(list);
    }

    public void StartAllFolderTimers()
    {
        foreach (var folder in _settingsService.AppSettings.LocalMediaFolders)
            if (folder.IsEnabled)
                UpdateFolderTimer(folder);
    }

    public void Receive(PropertyChangedMessage<AutoScanInterval> message)
    {
        if (message.Sender is MediaFolder mediaFolder)
            if (message.PropertyName == nameof(MediaFolder.ScanInterval))
                UpdateFolderTimer(mediaFolder);
    }

    public void Receive(PropertyChangedMessage<bool> message)
    {
        if (message.Sender is MediaFolder mediaFolder)
            if (message.PropertyName == nameof(MediaFolder.IsEnabled))
                UpdateFolderTimer(mediaFolder);
    }

    private async Task<List<FilesIndexItem>> SyncAsync(IUnifiedFileSystem provider, FilesIndexItem? parentFolder,
        string configId, bool forceSync = false)
    {
        List<FilesIndexItem> remoteItems;
        try
        {
            remoteItems = await provider.GetFilesAsync(parentFolder);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Network sync error: {ex.Message}");
            return [];
        }

        if (remoteItems == null) return [];

        var targetParentUri = "";
        if (remoteItems.Count > 0)
            targetParentUri = remoteItems[0].ParentUri ?? "";
        else if (parentFolder != null)
            targetParentUri = parentFolder.Uri;
        else
            return [];

        try
        {
            var col = GetCollection();

            var dbItems = col.Find(x => x.MediaFolderId == configId && x.ParentUri == targetParentUri).ToList();
            var dbMap = dbItems.ToDictionary(x => x.Uri, x => x);

            var remoteDistinct = remoteItems
                .GroupBy(x => x.Uri)
                .Select(g => g.First())
                .ToList();

            var remoteUris = new HashSet<string>();

            foreach (var remote in remoteDistinct)
            {
                remoteUris.Add(remote.Uri);

                if (dbMap.TryGetValue(remote.Uri, out var existing))
                {
                    bool lastModifiedTimeChanged = existing.LastModified != remote.LastModified;
                    if (existing.LastModified.HasValue && remote.LastModified.HasValue)
                    {
                        lastModifiedTimeChanged = Math.Abs((existing.LastModified.Value - remote.LastModified.Value).TotalSeconds) > 1;
                    }

                    bool createTimeChanged = existing.DateCreated != remote.DateCreated;
                    if (existing.DateCreated.HasValue && remote.DateCreated.HasValue)
                    {
                        createTimeChanged = Math.Abs((existing.DateCreated.Value - remote.DateCreated.Value).TotalSeconds) > 1;
                    }

                    var isChanged = existing.FileSize != remote.FileSize ||
                        createTimeChanged ||
                        lastModifiedTimeChanged ||
                        forceSync;

                    if (isChanged)
                    {
                        existing.FileSize = remote.FileSize;
                        existing.LastModified = remote.LastModified;
                        existing.DateCreated = remote.DateCreated;
                        existing.IsMetadataParsed = false;

                        col.Update(existing);
                    }
                }
                else
                {
                    col.Insert(remote);
                }
            }

            foreach (var dbItem in dbItems)
            {
                if (!remoteUris.Contains(dbItem.Uri))
                {
                    col.Delete(dbItem.Id);
                }
            }

            var finalItems = col.Find(x => x.MediaFolderId == configId && x.ParentUri == targetParentUri).ToList();

            FolderUpdated?.Invoke(this, targetParentUri);

            return finalItems;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Database sync error: {ex.Message}");
            return [];
        }
    }

    private void UpdateFolderTimer(MediaFolder folder)
    {
        if (_folderTimerTokens.TryRemove(folder.Id, out var oldCts))
        {
            oldCts.Cancel();
            oldCts.Dispose();
        }

        if (!folder.IsEnabled || folder.ScanInterval == AutoScanInterval.Disabled) return;

        var newCts = new CancellationTokenSource();
        _folderTimerTokens[folder.Id] = newCts;

        var period = folder.ScanInterval switch
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
                    await ScanMediaFolderAsync(folder, token: newCts.Token);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"文件夹 {folder.Name} 定时扫描出错: {ex.Message}");
            }
        }, newCts.Token);
    }

    public event EventHandler<string>? FolderUpdated;

    private async Task<string?> SaveAlbumArtToDiskAsync(ExtendedTrack track)
    {
        var picData = track.AlbumArtByteArray;
        if (picData == null || picData.Length == 0) return null;

        try
        {
            var hash = ComputeHashForBytes(picData);
            var safeName = hash + ".jpg";

            var localPath = Path.Combine(PathHelper.LocalAlbumArtCacheDirectory, safeName);

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
        using (var md5 = MD5.Create())
        {
            var hashBytes = md5.ComputeHash(data);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
