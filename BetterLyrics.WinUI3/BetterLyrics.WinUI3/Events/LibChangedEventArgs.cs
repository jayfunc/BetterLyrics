// 2025/6/23 by Zhe Fang

using System;
using System.IO;

namespace BetterLyrics.WinUI3.Events
{
    public class LibChangedEventArgs(string folder, string filePath, WatcherChangeTypes changeType) : EventArgs
    {
        public WatcherChangeTypes ChangeType { get; } = changeType;
        public string FilePath { get; } = filePath;
        public string Folder { get; } = folder;
    }
}
