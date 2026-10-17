using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.MediaLibrary;

public sealed class MediaFolderRealTimeScanChangedMessage(MediaFolder folder, bool isRealTimeScanEnabled)
{
    public MediaFolder Folder { get; } = folder;
    public bool IsRealTimeScanEnabled { get; } = isRealTimeScanEnabled;
}

