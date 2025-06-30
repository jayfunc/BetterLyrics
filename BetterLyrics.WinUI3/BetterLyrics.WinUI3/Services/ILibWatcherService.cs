// 2025/6/23 by Zhe Fang

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Events;
using BetterLyrics.WinUI3.Models;

namespace BetterLyrics.WinUI3.Services
{
    public interface ILibWatcherService
    {
        event EventHandler<LibChangedEventArgs>? MusicLibraryFilesChanged;

        public void UpdateWatchers(List<LocalLyricsFolder> folders);
    }
}
