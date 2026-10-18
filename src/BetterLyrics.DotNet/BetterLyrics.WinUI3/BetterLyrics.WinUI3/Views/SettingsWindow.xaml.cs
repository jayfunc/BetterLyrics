using BetterLyrics.Core.Enums;
using BetterLyrics.WinUI3.Extensions;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3.Views;

public sealed partial class SettingsWindow : BaseWindow
{
    public SettingsWindow()
    {
        InitializeComponent();
        this.Init("SettingsPageTitle");
    }

    private void MusicGalleryButton_Click(object sender, RoutedEventArgs e)
    {
        _windowManagerProvider.OpenOrShowWindow(WindowType.MusicGalleryWindow);
    }

    private void LyricsWindowSwitchButton_Click(object sender, RoutedEventArgs e)
    {
        _windowManagerProvider.OpenOrShowWindow(WindowType.LyricsWindowSwitchWindow);
    }
}