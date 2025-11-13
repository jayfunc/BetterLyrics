// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Events;
using System;

namespace BetterLyrics.WinUI3.Services.LibWatcherService
{
    public interface ILibWatcherService
    {
        event EventHandler<LibChangedEventArgs>? MusicLibraryFilesChanged;
    }
}
