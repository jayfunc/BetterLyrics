using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace BetterLyrics.WinUI3.Events
{
    public class FileChangedEventArgs(string folderId, string filePath, WatcherChangeTypes changeType) : EventArgs
    {
        public WatcherChangeTypes ChangeType { get; } = changeType;
        public string FilePath { get; } = filePath;
        public string FolderId { get; } = folderId;
    }
}
