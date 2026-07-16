using System;
using System.Threading.Tasks;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using BetterLyrics.WinUI3.Extensions;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views;

/// <summary>
///     An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MusicGalleryWindow : Window,
    IRecipient<PropertyChangedMessage<byte[]?>>,
    IRecipient<PropertyChangedMessage<AppTheme>>,
    IRecipient<PropertyChangedMessage<PaletteGeneratorType>>
{
    private readonly IGsmtcService _gsmtcService = Ioc.Default.GetRequiredService<IGsmtcService>();

    private readonly IWindowManagerProvider _windowManagerProvider =
        Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    public MusicGalleryWindow()
    {
        InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<MusicGalleryWindowViewModel>();
        this.Init("MusicGalleryPageTitle");

        AppWindow.Closing += AppWindow_Closing;

        WeakReferenceMessenger.Default.RegisterAll(this);

        _ = UpdateAlbumArtThemeColorsAsync();
    }

    public MusicGalleryWindowViewModel ViewModel { get; }

    public void Receive(PropertyChangedMessage<AppTheme> message)
    {
        if (message.Sender == ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus)
        {
            if (message.PropertyName == nameof(LyricsWindowStatus.WindowTheme)) _ = UpdateAlbumArtThemeColorsAsync();
        }
        else if (message.Sender is GeneralSettings)
        {
            if (message.PropertyName == nameof(GeneralSettings.AppTheme)) UpdateTheme();
        }
    }

    public void Receive(PropertyChangedMessage<byte[]?> message)
    {
        if (message.Sender is IGsmtcService)
            if (message.PropertyName == nameof(IGsmtcService.AlbumArtBytes))
                _ = UpdateAlbumArtThemeColorsAsync();
    }

    public void Receive(PropertyChangedMessage<PaletteGeneratorType> message)
    {
        if (message.Sender == ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus)
            if (message.PropertyName == nameof(LyricsWindowStatus.PaletteGeneratorType))
                _ = UpdateAlbumArtThemeColorsAsync();
    }

    public void Receive(PropertyChangedMessage<double> message)
    {
        if (message.Sender == ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus)
        {
            if (message.PropertyName == nameof(LyricsWindowStatus.PaletteChromaWeight) ||
                message.PropertyName == nameof(LyricsWindowStatus.PaletteToneWeight) ||
                message.PropertyName == nameof(LyricsWindowStatus.PalettePopulationWeight))
                _ = UpdateAlbumArtThemeColorsAsync();
        }
    }

    private void UpdateTheme()
    {
        var elementTheme = ViewModel.AppSettings.GeneralSettings.AppTheme.ToElementTheme();
        RootGrid.RequestedTheme = elementTheme;
        if (NowPlayingPage.Opacity == 1)
            NowPlayingBar.RequestedTheme = ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus
                .WindowPalette.ThemeType.ToElementTheme();
        else
            NowPlayingBar.RequestedTheme = elementTheme;

        AppWindow.TitleBar.PreferredTheme = NowPlayingBar.RequestedTheme.ToTitleBarTheme();
    }

    private async Task UpdateAlbumArtThemeColorsAsync()
    {
        var result = await _gsmtcService.CalculateAlbumArtThemeColorsAsync(
            ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus, Colors.Transparent);

        NowPlayingPage.LyricsWindowStatus?.WindowPalette = result;
        NowPlayingPage.RequestedTheme = result.ThemeType.ToElementTheme();

        UpdateTheme();
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (ViewModel.AppSettings.MusicGallerySettings.ExitOnWindowClosed)
            _windowManagerProvider.ExitApp();
        else
            _windowManagerProvider.PrepareWindowClosing(this);
    }

    private void NowPlayingBar_SongInfoTapped(object sender, EventArgs e)
    {
        NowPlayingBar.ShowSongInfo = false;
        NowPlayingBar.ShowTime = true;
        NowPlayingBar.IsAutoHideEnabled = true;
        NowPlayingPage.Visibility = Visibility.Visible;
        NowPlayingPage.Opacity = 1;
        UpdateTheme();
    }

    private async void NowPlayingBar_TimeTapped(object sender, EventArgs e)
    {
        NowPlayingBar.ShowSongInfo = true;
        NowPlayingBar.ShowTime = false;
        NowPlayingBar.IsAutoHideEnabled = false;
        NowPlayingPage.Opacity = 0;
        await Task.Delay(Time.AnimationDuration);
        NowPlayingPage.Visibility = Visibility.Collapsed;
        UpdateTheme();
    }

    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus.WindowStatus = WindowStatus.Opened;
    }

    private void RootGrid_Unloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus.WindowStatus = WindowStatus.Closed;
    }

    private void NowPlayingBar_PlayingQueueClick(object sender, EventArgs e)
    {
        if (NowPlayingPage.Visibility == Visibility.Visible)
        {
            if (PlayQueueFlyout.IsOpen)
                PlayQueueFlyout.Hide();
            else
                PlayQueueFlyout.ShowAt(NowPlayingBar);
        }
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        Closed -= Window_Closed;

        WeakReferenceMessenger.Default.UnregisterAll(this);

        AppWindow.Closing -= AppWindow_Closing;
    }
}