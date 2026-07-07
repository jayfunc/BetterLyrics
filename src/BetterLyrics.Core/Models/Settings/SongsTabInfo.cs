using BetterLyrics.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public class SongsTabInfo : ObservableRecipient
{
    public string Name { get; set; } = "";

    public string Icon { get; set; } = "";

    public CommonSongProperty FilterProperty { get; set; } = CommonSongProperty.Title;

    public string FilterValue { get; set; } = "";

    public bool IsDefault => Icon == "\uE8A9";
}