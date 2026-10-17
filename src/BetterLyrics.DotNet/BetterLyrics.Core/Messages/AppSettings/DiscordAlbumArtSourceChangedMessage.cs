using BetterLyrics.Core.Enums;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Core.Messages.AppSettings;

public sealed class DiscordAlbumArtSourceChangedMessage(OnlineAlbumArtProvider value)
    : ValueChangedMessage<OnlineAlbumArtProvider>(value);

