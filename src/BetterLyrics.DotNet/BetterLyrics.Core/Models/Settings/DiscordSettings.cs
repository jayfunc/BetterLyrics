using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Messages.AppSettings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace BetterLyrics.Core.Models.Settings;

public partial class DiscordSettings : ObservableObject
{
    [ObservableProperty]
    public partial OnlineAlbumArtProvider AlbumArtSource { get; set; } = OnlineAlbumArtProvider.None;

    partial void OnAlbumArtSourceChanged(OnlineAlbumArtProvider value)
    {
        WeakReferenceMessenger.Default.Send(new DiscordAlbumArtSourceChangedMessage(value));
    }
}
