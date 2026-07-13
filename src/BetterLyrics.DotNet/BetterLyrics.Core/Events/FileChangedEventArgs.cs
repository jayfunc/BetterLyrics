namespace BetterLyrics.Core.Events;

public class FileChangedEventArgs(string folderId, string filePath, WatcherChangeTypes changeType) : EventArgs
{
    public WatcherChangeTypes ChangeType { get; } = changeType;
    public string FilePath { get; } = filePath;
    public string FolderId { get; } = folderId;
}