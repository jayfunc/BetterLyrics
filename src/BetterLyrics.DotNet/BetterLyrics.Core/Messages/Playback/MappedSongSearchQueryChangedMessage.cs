using BetterLyrics.Core.Models.Entities;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Core.Messages.Playback;

public sealed class MappedSongSearchQueryChangedMessage(MappedSongSearchQuery? value)
    : ValueChangedMessage<MappedSongSearchQuery?>(value);

