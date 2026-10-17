using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Core.Messages.Playback;

public sealed class CurrentMediaSourceProviderChangedMessage(MediaSourceProviderInfo? value)
    : ValueChangedMessage<MediaSourceProviderInfo?>(value);

