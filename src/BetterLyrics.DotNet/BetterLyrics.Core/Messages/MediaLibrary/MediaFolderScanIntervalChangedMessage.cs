using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.MediaLibrary;

public sealed class MediaFolderScanIntervalChangedMessage(MediaFolder folder, AutoScanInterval scanInterval)
{
    public MediaFolder Folder { get; } = folder;
    public AutoScanInterval ScanInterval { get; } = scanInterval;
}

