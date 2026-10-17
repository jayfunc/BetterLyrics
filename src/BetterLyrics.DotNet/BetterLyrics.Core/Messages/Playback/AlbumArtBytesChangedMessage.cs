using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Core.Messages.Playback;

public sealed class AlbumArtBytesChangedMessage(byte[]? value) : ValueChangedMessage<byte[]?>(value);

