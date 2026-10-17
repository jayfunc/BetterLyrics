using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Core.Messages.Playback;

public sealed class SongScrobbledMessage(bool value) : ValueChangedMessage<bool>(value);

