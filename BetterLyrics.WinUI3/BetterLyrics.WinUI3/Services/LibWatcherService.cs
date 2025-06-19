using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using BetterLyrics.WinUI3.Services;
    using global::BetterLyrics.WinUI3.Events;

    namespace BetterLyrics.WinUI3.Services
    {
        public class LibWatcherService : IDisposable, ILibWatcherService
        {
            private readonly ISettingsService _settingsService;
            private readonly Dictionary<string, FileSystemWatcher> _watchers = [];

            public event EventHandler<LibChangedEventArgs>? MusicLibraryFilesChanged;

            public LibWatcherService(ISettingsService settingsService)
            {
                _settingsService = settingsService;
                UpdateWatchers(_settingsService.MusicLibraries);
            }

            public void UpdateWatchers(List<string> folders)
            {
                // 移除不再监听的
                foreach (var key in _watchers.Keys.ToList())
                {
                    if (!folders.Contains(key))
                    {
                        _watchers[key].Dispose();
                        _watchers.Remove(key);
                    }
                }

                // 添加新的监听
                foreach (var folder in folders)
                {
                    if (!_watchers.ContainsKey(folder) && Directory.Exists(folder))
                    {
                        var watcher = new FileSystemWatcher(folder)
                        {
                            IncludeSubdirectories = true,
                            EnableRaisingEvents = true,
                        };
                        watcher.Created += (s, e) => OnChanged(folder, e);
                        watcher.Changed += (s, e) => OnChanged(folder, e);
                        watcher.Deleted += (s, e) => OnChanged(folder, e);
                        watcher.Renamed += (s, e) => OnChanged(folder, e);
                        _watchers[folder] = watcher;
                    }
                }
            }

            private void OnChanged(string folder, FileSystemEventArgs e)
            {
                MusicLibraryFilesChanged?.Invoke(
                    this,
                    new LibChangedEventArgs(folder, e.FullPath, e.ChangeType)
                );
            }

            public void Dispose()
            {
                foreach (var watcher in _watchers.Values)
                {
                    watcher.Dispose();
                }
                _watchers.Clear();
            }
        }
    }
}
