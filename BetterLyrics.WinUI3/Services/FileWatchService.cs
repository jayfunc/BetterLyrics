using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Events;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services
{
    public partial class FileWatchService : BaseViewModel, IDisposable, IFileWatchService,
         IRecipient<PropertyChangedMessage<bool>>
    {
        private readonly Dictionary<string, FileSystemWatcher> _watchers = [];
        private readonly ConcurrentDictionary<string, CancellationTokenSource> _debounceTokens = new();

        private readonly ISettingsService _settingsService;
        private readonly IFileSystemService _fileSystemService;

        private const int DebounceDelay = 2000;

        public FileWatchService(ISettingsService settingsService, IFileSystemService fileSystemService)
        {
            _settingsService = settingsService;
            _fileSystemService = fileSystemService;

            _settingsService.AppSettings.LocalMediaFolders.CollectionChanged += LocalMediaFolders_CollectionChanged;
            UpdateWatchers();
        }

        private void LocalMediaFolders_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                UpdateWatchers();
            }
        }

        public event EventHandler<FileChangedEventArgs>? MusicLibraryFilesChanged;

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

        private void UpdateWatchers()
        {
            var folders = _settingsService.AppSettings.LocalMediaFolders
                .Where(x => x.IsEnabled && x.SourceType == FileSourceType.Local && x.IsRealTimeScanEnabled).ToList();

            foreach (var key in _watchers.Keys.ToList())
            {
                if (!folders.Any(x => x.Id == key))
                {
                    _watchers[key].EnableRaisingEvents = false;
                    _watchers[key].Dispose();
                    _watchers.Remove(key);

                    if (_debounceTokens.TryRemove(key, out var cts))
                    {
                        cts.Cancel();
                        cts.Dispose();
                    }
                }
            }

            foreach (var folder in folders)
            {
                if (!_watchers.ContainsKey(folder.Id))
                {
                    try
                    {
                        if (Directory.Exists(folder.UriPath))
                        {
                            var watcher = new FileSystemWatcher(folder.UriPath)
                            {
                                IncludeSubdirectories = true,
                                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.DirectoryName | NotifyFilters.Size
                            };

                            watcher.Created += (s, e) => OnChangedWithDebounce(folder, e);
                            watcher.Changed += (s, e) => OnChangedWithDebounce(folder, e);
                            watcher.Deleted += (s, e) => OnChangedWithDebounce(folder, e);
                            watcher.Renamed += (s, e) => OnChangedWithDebounce(folder, e);

                            watcher.EnableRaisingEvents = true;
                            _watchers[folder.Id] = watcher;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to start watcher for {folder.UriPath}: {ex.Message}");
                    }
                }
            }
        }

        private void OnChangedWithDebounce(MediaFolder folder, FileSystemEventArgs e)
        {
            if (e.Name != null && (e.Name.StartsWith("~$") || e.Name.EndsWith(".tmp"))) return;

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
                    System.Diagnostics.Debug.WriteLine($"FileWatch Error: {ex.Message}");
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

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is MediaFolder)
            {
                if (message.PropertyName == nameof(MediaFolder.IsRealTimeScanEnabled) ||
                    message.PropertyName == nameof(MediaFolder.IsEnabled))
                {
                    UpdateWatchers();
                }
            }
        }
    }
}