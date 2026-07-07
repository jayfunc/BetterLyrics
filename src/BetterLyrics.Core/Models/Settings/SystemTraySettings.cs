using BetterLyrics.Core.Enums;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class SystemTraySettings : ObservableRecipient
{
    [ObservableProperty]
    public partial SystemTrayClickCallback SystemTrayClickCallback { get; set; } =
        SystemTrayClickCallback.LyricsWindowSwitchWindow;

    [ObservableProperty]
    public partial SystemTrayClickCallback SystemTrayDoubleClickCallback { get; set; } = SystemTrayClickCallback.None;

    [ObservableProperty]
    public partial SystemTrayClickCallback SystemTrayMiddleClickCallback { get; set; } = SystemTrayClickCallback.None;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool ColorfulSystemTrayIcon { get; set; } = true;
}