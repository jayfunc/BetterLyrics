using BetterLyrics.Core.Enums;

namespace BetterLyrics.Core.Messages.SettingsNavigation;

public sealed class LyricsWindowNavigationRequestedMessage(LyricsWindowManagerSettingsSection targetSection, Guid? targetWindowId = null)
{
    public LyricsWindowManagerSettingsSection TargetSection { get; } = targetSection;
    public Guid? TargetWindowId { get; } = targetWindowId;
}
