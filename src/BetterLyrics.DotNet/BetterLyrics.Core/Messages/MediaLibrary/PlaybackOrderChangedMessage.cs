using BetterLyrics.Core.Enums;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Core.Messages.MediaLibrary;

public sealed class PlaybackOrderChangedMessage(PlaybackOrder value) : ValueChangedMessage<PlaybackOrder>(value);

