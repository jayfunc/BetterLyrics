using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.MediaLibrary;

public sealed class MediaFolderProcessingChangedMessage(MediaFolder folder, bool isProcessing)
{
    public MediaFolder Folder { get; } = folder;
    public bool IsProcessing { get; } = isProcessing;
}

