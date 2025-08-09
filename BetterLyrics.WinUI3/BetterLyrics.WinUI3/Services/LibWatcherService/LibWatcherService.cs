// 2025/6/23 by Zhe Fang

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.ViewModels;
using Microsoft.UI.Dispatching;

namespace BetterLyrics.WinUI3.Services.LibWatcherService
{
    public class LibWatcherService : BaseViewModel, IDisposable, ILibWatcherService
    {
        private readonly Dictionary<string, FileSystemWatcher> _watchers = [];

        public LibWatcherService(ISettingsService settingsService) : base(settingsService)
        {
            UpdateWatchers(_settingsService.AppSettings.LocalMediaFolders);
        }

        public event EventHandler<LibChangedEventArgs>? MusicLibraryFilesChanged;

        public void Dispose()
        {
            foreach (var watcher in _watchers.Values)
            {
                watcher.Dispose();
            }
            _watchers.Clear();
        }

        public void UpdateWatchers(List<LocalMediaFolder> folders)
        {
            // 移除不再监听的
            foreach (var key in _watchers.Keys.ToList())
            {
                if (!folders.Any(x => x.Path == key && x.IsEnabled))
                {
                    _watchers[key].Dispose();
                    _watchers.Remove(key);
                }
            }

            // 添加新的监听
            foreach (var folder in folders)
            {
                if (
                    !_watchers.ContainsKey(folder.Path)
                    && Directory.Exists(folder.Path)
                    && folder.IsEnabled
                )
                {
                    var watcher = new FileSystemWatcher(folder.Path)
                    {
                        IncludeSubdirectories = true,
                        EnableRaisingEvents = true,
                    };
                    watcher.Created += (s, e) => OnChanged(folder.Path, e);
                    watcher.Changed += (s, e) => OnChanged(folder.Path, e);
                    watcher.Deleted += (s, e) => OnChanged(folder.Path, e);
                    watcher.Renamed += (s, e) => OnChanged(folder.Path, e);
                    _watchers[folder.Path] = watcher;
                }
            }
        }

        private void OnChanged(string folder, FileSystemEventArgs e)
        {
            _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                MusicLibraryFilesChanged?.Invoke(
                    this,
                    new LibChangedEventArgs(folder, e.FullPath, e.ChangeType)
                );
            });
        }
    }
}
