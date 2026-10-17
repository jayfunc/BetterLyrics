using BetterLyrics.Core.Models.Lyrics;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Core.Messages.Playback;

public sealed class CurrentLyricsDataChangedMessage(LyricsData? value) : ValueChangedMessage<LyricsData?>(value);

