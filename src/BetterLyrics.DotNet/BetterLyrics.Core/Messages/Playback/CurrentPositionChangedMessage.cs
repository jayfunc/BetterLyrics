using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Core.Messages.Playback;

public sealed class CurrentPositionChangedMessage(TimeSpan value) : ValueChangedMessage<TimeSpan>(value);

