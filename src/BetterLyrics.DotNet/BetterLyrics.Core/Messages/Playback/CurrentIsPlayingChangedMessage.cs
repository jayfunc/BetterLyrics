using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Core.Messages.Playback;

public sealed class CurrentIsPlayingChangedMessage(bool value) : ValueChangedMessage<bool>(value);

