using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Messages.AppSettings;
using BetterLyrics.WinUI3.Extensions;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3.Views;

public sealed partial class SettingsWindow : Window,
    IRecipient<AppThemeChangedMessage>
{
    private readonly IWindowManagerProvider _windowManagerProvider = Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    public SettingsWindow()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.RegisterAll(this);

        this.Init("SettingsPageTitle");
        this.SyncTheme();

        AppWindow.Closing += AppWindow_Closing;
    }

    ~SettingsWindow()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    public void Receive(AppThemeChangedMessage message)
    {
        this.SyncTheme();
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        _windowManagerProvider.CloseWindow(this);
    }

    private void MusicGalleryButton_Click(object sender, RoutedEventArgs e)
    {
        _windowManagerProvider.OpenOrShowWindow<MusicGalleryWindow>();
    }

    private void LyricsWindowSwitchButton_Click(object sender, RoutedEventArgs e)
    {
        _windowManagerProvider.OpenOrShowWindow<LyricsWindowSwitchWindow>();
    }
}