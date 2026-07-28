using BetterLyrics.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class DiscordSettings : ObservableRecipient
{
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial DiscordAlbumArtSource AlbumArtSource { get; set; } = DiscordAlbumArtSource.None;
}
