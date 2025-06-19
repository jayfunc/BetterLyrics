using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Events
{
    public class LibChangedEventArgs : EventArgs
    {
        public string Folder { get; }
        public string FilePath { get; }
        public WatcherChangeTypes ChangeType { get; }

        public LibChangedEventArgs(string folder, string filePath, WatcherChangeTypes changeType)
        {
            Folder = folder;
            FilePath = filePath;
            ChangeType = changeType;
        }
    }
}
