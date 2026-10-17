using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.MediaLibrary;

public sealed class MediaSourceDiscordPresenceChangedMessage(
    MediaSourceProviderInfo provider,
    bool isDiscordPresenceEnabled)
{
    public MediaSourceProviderInfo Provider { get; } = provider;
    public bool IsDiscordPresenceEnabled { get; } = isDiscordPresenceEnabled;
}

