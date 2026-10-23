using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Messages.AppSettings;
using BetterLyrics.Core.Messages.LyricsWindow;
using BetterLyrics.Core.Messages.Playback;
using BetterLyrics.Core.ViewModels;
using BetterLyrics.WinUI3.Extensions;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Vanara.PInvoke;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MusicGalleryWindow : BaseWindow,
    IRecipient<AlbumArtBytesChangedMessage>,
    IRecipient<LyricsWindowPaletteConfigChangedMessage>,
    IRecipient<CurrentMediaSourceProviderChangedMessage>
{
    public IGsmtcService GSMTCService { get; } = Ioc.Default.GetRequiredService<IGsmtcService>();

    public MusicGalleryWindowViewModel ViewModel { get; }

    public MusicGalleryWindow()
    {
        InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<MusicGalleryWindowViewModel>();
        this.Init("MusicGalleryPageTitle");

        NowPlayingPage.LyricsWindowStatus = ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus;
        UpdateExternalSourceVisibility();

        AppWindow.Closing += AppWindow_Closing;

        _ = UpdateAlbumArtThemeColorsAsync();
    }

    protected override void OnAppThemeChanged(AppThemeChangedMessage? message = null)
    {
        this.UpdateTheme();
    }

    protected override IEnumerable<object> GetAdditionalTrackedViewModels() => [ViewModel];

    private void UpdateExternalSourceVisibility()
    {
        var isActive = GSMTCService.IsExternalSourceActive;
        NowPlayingBar.Visibility = isActive ? Visibility.Collapsed : Visibility.Visible;
        ExternalSourcePromptGrid.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
    }

    private void UpdateTheme()
    {
        var elementTheme = ViewModel.AppSettings.GeneralSettings.AppTheme.ToElementTheme();
        RootGrid.RequestedTheme = elementTheme;
        if (NowPlayingPage.Opacity == 1)
        {
            NowPlayingBar.RequestedTheme = ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus.WindowPalette.ThemeType.ToElementTheme();
        }
        else
        {
            NowPlayingBar.RequestedTheme = elementTheme;
        }

        AppWindow.TitleBar.PreferredTheme = NowPlayingBar.RequestedTheme.ToTitleBarTheme();
    }

    private async Task UpdateAlbumArtThemeColorsAsync()
    {
        try
        {
            var result = await GSMTCService.CalculateAlbumArtThemeColorsAsync(ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus, Colors.Transparent);

            NowPlayingPage.LyricsWindowStatus?.WindowPalette = result;
            NowPlayingPage.RequestedTheme = result.ThemeType.ToElementTheme();

            UpdateTheme();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateAlbumArtThemeColorsAsync: {ex}");
        }
    }

    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (ViewModel.AppSettings.MusicGallerySettings.ExitOnWindowClosed)
        {
            _windowManagerProvider.ExitApp();
            return;
        }
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
        ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus.WindowState = LyricsWindowState.Opened;
    }

    private void RootGrid_Unloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus.WindowState = LyricsWindowState.Closed;
    }

    private void NowPlayingBar_PlayingQueueClick(object sender, EventArgs e)
    {
        if (NowPlayingPage.Visibility == Visibility.Visible)
        {
            if (PlayQueueFlyout.IsOpen)
            {
                PlayQueueFlyout.Hide();
            }
            else
            {
                PlayQueueFlyout.ShowAt(NowPlayingBar);
            }
        }
    }

    public void Receive(AlbumArtBytesChangedMessage message)
    {
        _ = UpdateAlbumArtThemeColorsAsync();
    }

    public void Receive(LyricsWindowPaletteConfigChangedMessage message)
    {
        if (message.Sender == ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus)
            _ = UpdateAlbumArtThemeColorsAsync();
    }

    public void Receive(CurrentMediaSourceProviderChangedMessage message)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            UpdateExternalSourceVisibility();
            if (GSMTCService.IsExternalSourceActive)
            {
                if (NowPlayingPage.Visibility == Visibility.Visible)
                {
                    NowPlayingBar_TimeTapped(null, EventArgs.Empty);
                }
            }
        });
    }
}