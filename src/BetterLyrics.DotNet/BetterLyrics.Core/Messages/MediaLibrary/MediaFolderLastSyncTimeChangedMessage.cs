using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.MediaLibrary;

public sealed class MediaFolderLastSyncTimeChangedMessage(MediaFolder folder, DateTime? lastSyncTime)
{
    public MediaFolder Folder { get; } = folder;
    public DateTime? LastSyncTime { get; } = lastSyncTime;
}

