using BetterLyrics.Core.Models;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Core.Messages.Playback;

public sealed class CurrentSongInfoChangedMessage(SongInfo value) : ValueChangedMessage<SongInfo>(value);

