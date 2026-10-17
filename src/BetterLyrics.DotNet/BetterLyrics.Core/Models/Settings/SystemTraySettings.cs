using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Messages.AppSettings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace BetterLyrics.Core.Models.Settings;

public partial class SystemTraySettings : ObservableObject
{
    [ObservableProperty]
    public partial SystemTrayClickCallback SystemTrayClickCallback { get; set; } =
        SystemTrayClickCallback.LyricsWindowSwitchWindow;

    [ObservableProperty]
    public partial SystemTrayClickCallback SystemTrayDoubleClickCallback { get; set; } = SystemTrayClickCallback.None;

    [ObservableProperty]
    public partial SystemTrayClickCallback SystemTrayMiddleClickCallback { get; set; } = SystemTrayClickCallback.None;

    [ObservableProperty]
    public partial bool ColorfulSystemTrayIcon { get; set; } = true;

    partial void OnColorfulSystemTrayIconChanged(bool value)
    {
        WeakReferenceMessenger.Default.Send(new ColorfulSystemTrayIconChangedMessage(value));
    }
}