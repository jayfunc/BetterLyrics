using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Events;

namespace BetterLyrics.WinUI3.Services
{
    public interface ILibWatcherService
    {
        event EventHandler<LibChangedEventArgs>? MusicLibraryFilesChanged;
        public void UpdateWatchers(List<string> folders);
    }
}
