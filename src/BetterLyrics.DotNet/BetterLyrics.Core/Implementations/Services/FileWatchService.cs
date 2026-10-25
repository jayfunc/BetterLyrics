using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Events;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Messages.MediaLibrary;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Collections.Specialized;

namespace BetterLyrics.Core.Implementations.Services;

/// <summary>
/// Singleton Service
/// </summary>
public class FileWatchService : IDisposable, IFileWatchService,
    IRecipient<MediaFolderRealTimeScanChangedMessage>,
    IRecipient<MediaFolderEnabledChangedMessage>
{
    private const int DebounceDelay = 2000;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _debounceTokens = new();
    private readonly IFileSystemService _fileSystemService;
    private readonly ILogger<FileWatchService> _logger;

    private readonly ISettingsService _settingsService;
    private readonly Dictionary<string, FileSystemWatcher> _watchers = [];

    public FileWatchService(ISettingsService settingsService, IFileSystemService fileSystemService,
        ILogger<FileWatchService> logger)
    {
        _settingsService = settingsService;
        _fileSystemService = fileSystemService;
        _logger = logger;

        _settingsService.AppSettings.LocalMediaFolders.CollectionChanged += LocalMediaFolders_CollectionChanged;
        UpdateWatchers();

        WeakReferenceMessenger.Default.RegisterAll(this);
    }

    private void OnChangedWithDebounce(MediaFolder folder, FileSystemEventArgs e)
    {
        if (e.Name != null && (e.Name.StartsWith("~$") || e.Name.EndsWith(".tmp"))) return;

        _logger.LogDebug("File change detected in \"{Folder}\": {ChangeType} {Name}.", folder.Name, e.ChangeType,
            e.Name);

        var key = folder.Id;

        if (_debounceTokens.TryGetValue(key, out var oldCts))
        {
            if (!oldCts.IsCancellationRequested)
            {
                oldCts.Cancel();
            }
        }

        var newCts = new CancellationTokenSource();
        _debounceTokens.AddOrUpdate(key, newCts, (k, v) => newCts);

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DebounceDelay, newCts.Token);

                await _fileSystemService.ScanMediaFolderAsync(folder);
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Real-time scan failed for folder \"{Folder}\" triggered by {ChangeType}.",
                    folder.Name, e.ChangeType);
            }
            finally
            {
                if (_debounceTokens.TryGetValue(key, out var currentCts) && currentCts == newCts)
                {
                    _debounceTokens.TryRemove(key, out _);
                }
                newCts.Dispose();
            }
        }, newCts.Token);
    }

    public void Dispose()
    {
        foreach (var watcher in _watchers.Values)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }

        _watchers.Clear();

        foreach (var cts in _debounceTokens.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }

        _debounceTokens.Clear();
    }

    private void LocalMediaFolders_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Remove) UpdateWatchers();
    }

    public event EventHandler<FileChangedEventArgs>? MusicLibraryFilesChanged;

    private void UpdateWatchers()
    {
        var folders = _settingsService.AppSettings.LocalMediaFolders
            .Where(x => x.IsEnabled && x.SourceType == FileSourceType.Local && x.IsRealTimeScanEnabled).ToList();

        foreach (var key in _watchers.Keys.ToList())
            if (!folders.Any(x => x.Id == key))
            {
                _logger.LogDebug("Stopping file watcher for folder {FolderId}.", key);
                _watchers[key].EnableRaisingEvents = false;
                _watchers[key].Dispose();
                _watchers.Remove(key);

                if (_debounceTokens.TryRemove(key, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                }
            }

        foreach (var folder in folders)
            if (!_watchers.ContainsKey(folder.Id))
                try
                {
                    if (Directory.Exists(folder.UriPath))
                    {
                        var watcher = new FileSystemWatcher(folder.UriPath)
                        {
                            IncludeSubdirectories = true,
                            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite |
                                           NotifyFilters.DirectoryName | NotifyFilters.Size
                        };

                        watcher.Created += (s, e) => OnChangedWithDebounce(folder, e);
                        watcher.Changed += (s, e) => OnChangedWithDebounce(folder, e);
                        watcher.Deleted += (s, e) => OnChangedWithDebounce(folder, e);
                        watcher.Renamed += (s, e) => OnChangedWithDebounce(folder, e);

                        watcher.EnableRaisingEvents = true;
                        _watchers[folder.Id] = watcher;

                        _logger.LogDebug("Started file watcher for \"{Folder}\" at {Path}.", folder.Name,
                            folder.UriPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to start file watcher for \"{Folder}\" at {Path}.",
                        folder.Name, folder.UriPath);
                }
    }

    public void Receive(MediaFolderRealTimeScanChangedMessage message)
    {
        UpdateWatchers();
    }

    public void Receive(MediaFolderEnabledChangedMessage message)
    {
        UpdateWatchers();
    }
}