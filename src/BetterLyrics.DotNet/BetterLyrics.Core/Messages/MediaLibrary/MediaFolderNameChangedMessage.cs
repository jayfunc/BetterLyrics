using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.MediaLibrary;

public sealed class MediaFolderNameChangedMessage(MediaFolder folder, string name)
{
    public MediaFolder Folder { get; } = folder;
    public string Name { get; } = name;
}

