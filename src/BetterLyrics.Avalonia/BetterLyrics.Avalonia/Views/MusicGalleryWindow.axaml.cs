using System;
using System.Threading.Tasks;
using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Interactivity;
using global::Avalonia.Media;
using global::Avalonia.Threading;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using BetterLyrics.Avalonia.Extensions;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Avalonia.Views;

public partial class MusicGalleryWindow : Window,
    IRecipient<PropertyChangedMessage<byte[]?>>,
    IRecipient<PropertyChangedMessage<AppTheme>>,
    IRecipient<PropertyChangedMessage<PaletteGeneratorType>>
{
    private readonly IGsmtcService _gsmtcService = Ioc.Default.GetRequiredService<IGsmtcService>();
    private readonly IWindowManagerProvider _windowManagerProvider = Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    public MusicGalleryWindow()
    {
        InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<MusicGalleryWindowViewModel>();
        this.Init("MusicGalleryPageTitle");

        NowPlayingPage.LyricsWindowStatus = ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus;

        Closing += AppWindow_Closing;

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

    private void UpdateTheme()
    {
        var elementTheme = ViewModel.AppSettings.GeneralSettings.AppTheme.ToThemeVariant();
        this.RequestedThemeVariant = elementTheme;
        if (NowPlayingPage.Opacity == 1)
            NowPlayingBarScope.RequestedThemeVariant = ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus
                .WindowPalette.ThemeType.ToThemeVariant();
        else
            NowPlayingBarScope.RequestedThemeVariant = elementTheme;
    }

    private async Task UpdateAlbumArtThemeColorsAsync()
    {
        var result = await _gsmtcService.CalculateAlbumArtThemeColorsAsync(
            ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus, BetterLyrics.Core.Constants.Colors.Transparent);

        if (NowPlayingPage.LyricsWindowStatus != null)
        {
            NowPlayingPage.LyricsWindowStatus.WindowPalette = result;
        }
        NowPlayingPageScope.RequestedThemeVariant = result.ThemeType.ToThemeVariant();

        UpdateTheme();
    }

    private void AppWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (ViewModel.AppSettings.MusicGallerySettings.ExitOnWindowClosed)
        {
            _windowManagerProvider.ExitApp();
        }
        else
        {
            _windowManagerProvider.PrepareWindowClosing(this);
            e.Cancel = true;
        }
    }

    private void NowPlayingBar_SongInfoTapped(object? sender, EventArgs e)
    {
        NowPlayingBar.ShowSongInfo = false;
        NowPlayingBar.ShowTime = true;
        NowPlayingBar.IsAutoHideEnabled = true;
        NowPlayingPage.IsVisible = true;
        NowPlayingPage.Opacity = 1;
        UpdateTheme();
    }

    private async void NowPlayingBar_TimeTapped(object? sender, EventArgs e)
    {
        NowPlayingBar.ShowSongInfo = true;
        NowPlayingBar.ShowTime = false;
        NowPlayingBar.IsAutoHideEnabled = false;
        NowPlayingPage.Opacity = 0;
        await Task.Delay(Time.AnimationDuration);
        NowPlayingPage.IsVisible = false;
        UpdateTheme();
    }

    private void RootGrid_Loaded(object? sender, RoutedEventArgs e)
    {
        ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus.WindowStatus = WindowStatus.Opened;
    }

    private void RootGrid_Unloaded(object? sender, RoutedEventArgs e)
    {
        ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus.WindowStatus = WindowStatus.Closed;
    }

    private void NowPlayingBar_PlayingQueueClick(object? sender, EventArgs e)
    {
        // TODO: Port PlayQueue
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        WeakReferenceMessenger.Default.UnregisterAll(this);
        Closing -= AppWindow_Closing;
    }
}