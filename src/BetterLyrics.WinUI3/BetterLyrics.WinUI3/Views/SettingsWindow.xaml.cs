using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Extensions;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace BetterLyrics.WinUI3.Views;

public sealed partial class SettingsWindow : Window,
    IRecipient<PropertyChangedMessage<AppTheme>>
{
    private readonly IWindowManagerProvider _windowManagerProvider =
        Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    public SettingsWindow()
    {
        InitializeComponent();

        WeakReferenceMessenger.Default.RegisterAll(this);

        this.Init("SettingsPageTitle");
        this.SyncTheme();

        AppWindow.Closing += AppWindow_Closing;
    }

    public void Receive(PropertyChangedMessage<AppTheme> message)
    {
        if (message.Sender is GeneralSettings)
            if (message.PropertyName == nameof(GeneralSettings.AppTheme))
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