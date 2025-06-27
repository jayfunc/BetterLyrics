// 2025/6/23 by Zhe Fang

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Services
{
    using global::BetterLyrics.WinUI3.Events;
    using global::BetterLyrics.WinUI3.Models;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    namespace BetterLyrics.WinUI3.Services
    {
        /// <summary>
        /// Defines the <see cref="LibWatcherService" />
        /// </summary>
        public class LibWatcherService : IDisposable, ILibWatcherService
        {
            #region Fields

            /// <summary>
            /// Defines the _settingsService
            /// </summary>
            private readonly ISettingsService _settingsService;

            /// <summary>
            /// Defines the _watchers
            /// </summary>
            private readonly Dictionary<string, FileSystemWatcher> _watchers = [];

            #endregion

            #region Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="LibWatcherService"/> class.
            /// </summary>
            /// <param name="settingsService">The settingsService<see cref="ISettingsService"/></param>
            public LibWatcherService(ISettingsService settingsService)
            {
                _settingsService = settingsService;
                UpdateWatchers(_settingsService.LocalLyricsFolders);
            }

            #endregion

            #region Events

            /// <summary>
            /// Defines the MusicLibraryFilesChanged
            /// </summary>
            public event EventHandler<LibChangedEventArgs>? MusicLibraryFilesChanged;

            #endregion

            #region Methods

            /// <summary>
            /// The Dispose
            /// </summary>
            public void Dispose()
            {
                foreach (var watcher in _watchers.Values)
                {
                    watcher.Dispose();
                }
                _watchers.Clear();
            }

            /// <summary>
            /// The UpdateWatchers
            /// </summary>
            /// <param name="folders">The folders<see cref="List{LocalLyricsFolder}"/></param>
            public void UpdateWatchers(List<LocalLyricsFolder> folders)
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

            /// <summary>
            /// The OnChanged
            /// </summary>
            /// <param name="folder">The folder<see cref="string"/></param>
            /// <param name="e">The e<see cref="FileSystemEventArgs"/></param>
            private void OnChanged(string folder, FileSystemEventArgs e)
            {
                App.DispatcherQueue!.TryEnqueue(
                    Microsoft.UI.Dispatching.DispatcherQueuePriority.High,
                    () =>
                    {
                        MusicLibraryFilesChanged?.Invoke(
                            this,
                            new LibChangedEventArgs(folder, e.FullPath, e.ChangeType)
                        );
                    }
                );
            }

            #endregion
        }
    }
}
