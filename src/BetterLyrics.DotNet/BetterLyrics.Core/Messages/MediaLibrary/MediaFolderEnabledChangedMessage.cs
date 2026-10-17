using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.MediaLibrary;

public sealed class MediaFolderEnabledChangedMessage(MediaFolder folder, bool isEnabled)
{
    public MediaFolder Folder { get; } = folder;
    public bool IsEnabled { get; } = isEnabled;
}

