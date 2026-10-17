using BetterLyrics.Core.Enums;

namespace BetterLyrics.Core.Messages.SettingsNavigation;

public sealed class PlaybackLibSettingsNavigationRequestedMessage(PlaybackLibSettingsSection targetSection, string? targetSessionId = null)
{
    public PlaybackLibSettingsSection TargetSection { get; } = targetSection;
    public string? TargetSessionId { get; } = targetSessionId;
}
