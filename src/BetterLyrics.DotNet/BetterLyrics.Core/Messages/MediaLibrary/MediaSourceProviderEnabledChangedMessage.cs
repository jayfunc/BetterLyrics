using BetterLyrics.Core.Models.Settings;

namespace BetterLyrics.Core.Messages.MediaLibrary;

public sealed class MediaSourceProviderEnabledChangedMessage(MediaSourceProviderInfo provider, bool isEnabled)
{
    public MediaSourceProviderInfo Provider { get; } = provider;
    public bool IsEnabled { get; } = isEnabled;
}

