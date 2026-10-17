using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Core.Messages.AppSettings;

public sealed class MultiNowPlayingWindowModeChangedMessage(bool value) : ValueChangedMessage<bool>(value);

