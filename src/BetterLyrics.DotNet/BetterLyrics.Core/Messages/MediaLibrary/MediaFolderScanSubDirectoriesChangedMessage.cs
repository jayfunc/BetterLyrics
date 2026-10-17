using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.MediaLibrary;

public sealed class MediaFolderScanSubDirectoriesChangedMessage(MediaFolder folder, bool scanSubDirectories)
{
    public MediaFolder Folder { get; } = folder;
    public bool ScanSubDirectories { get; } = scanSubDirectories;
}

