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
using System.Threading.Tasks;
using Vanara.PInvoke;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views;

/// <summary>
///     An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MusicGalleryWindow : Window,
    IRecipient<AlbumArtBytesChangedMessage>,
    IRecipient<AppThemeChangedMessage>,
    IRecipient<LyricsWindowPaletteConfigChangedMessage>,
    IRecipient<CurrentMediaSourceProviderChangedMessage>
{
    public IGsmtcService GSMTCService { get; } = Ioc.Default.GetRequiredService<IGsmtcService>();

    private readonly IWindowManagerProvider _windowManagerProvider =
        Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    private bool _isClosing;
    private bool _isDisposed;

    public MusicGalleryWindow()
    {
        InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<MusicGalleryWindowViewModel>();
        this.Init("MusicGalleryPageTitle");

        NowPlayingPage.LyricsWindowStatus = ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus;
        UpdateExternalSourceVisibility();

        AppWindow.Closing += AppWindow_Closing;

        WeakReferenceMessenger.Default.RegisterAll(this);

        _ = UpdateAlbumArtThemeColorsAsync();
    }

    public MusicGalleryWindowViewModel ViewModel { get; }

    private void UpdateExternalSourceVisibility()
    {
        var isActive = GSMTCService.IsExternalSourceActive;
        NowPlayingBar.Visibility = isActive ? Visibility.Collapsed : Visibility.Visible;
        ExternalSourcePromptGrid.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
    }

    public void Receive(AppThemeChangedMessage message)
    {
        if (_isDisposed) return;

        UpdateTheme();
    }

    public void Receive(AlbumArtBytesChangedMessage message)
    {
        if (_isDisposed) return;

        _ = UpdateAlbumArtThemeColorsAsync();
    }

    public void Receive(LyricsWindowPaletteConfigChangedMessage message)
    {
        if (_isDisposed) return;

        if (message.Sender == ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus)
            _ = UpdateAlbumArtThemeColorsAsync();
    }

    public void Receive(CurrentMediaSourceProviderChangedMessage message)
    {
        if (_isDisposed) return;

        DispatcherQueue.TryEnqueue(() =>
        {
            if (_isDisposed) return;
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

    private void UpdateTheme()
    {
        if (_isDisposed) return;

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
        try
        {
            var result = await GSMTCService.CalculateAlbumArtThemeColorsAsync(
                ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus, Colors.Transparent);

            if (_isDisposed) return;

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

        if (!_isClosing && Content != null)
        {
            _isClosing = true;
            args.Cancel = true;

            AppWindow?.Hide();
            await Task.Delay(200);

            User32.PostMessage(WindowNative.GetWindowHandle(this), (uint)User32.WindowMessage.WM_CLOSE);
            return;
        }

        Cleanup();
        _windowManagerProvider.PrepareWindowClosing(this);
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        Cleanup();
    }

    private void Cleanup()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        Closed -= Window_Closed;
        AppWindow.Closing -= AppWindow_Closing;

        WeakReferenceMessenger.Default.UnregisterAll(this);

        DetachXamlEventHandlers();

        try
        {
            var smtcService = Ioc.Default.GetService<ISmtcService>();
            smtcService?.SaveCurrentPlaybackPosition();
        }
        catch { }

        if (PlayQueueFlyout != null)
        {
            if (PlayQueueFlyout.IsOpen)
                PlayQueueFlyout.Hide();
            NowPlayingBar.ContextFlyout = null;
        }

        NowPlayingPage?.Cleanup();
        MusicGalleryPage?.Cleanup();

        NowPlayingPage.LyricsWindowStatus = null;

        RootGrid.Children.Clear();
    }

    private void DetachXamlEventHandlers()
    {
        RootGrid.Loaded -= RootGrid_Loaded;
        RootGrid.Unloaded -= RootGrid_Unloaded;

        NowPlayingBar.PlayQueueButtonClick -= NowPlayingBar_PlayingQueueClick;
        NowPlayingBar.SongInfoTapped -= NowPlayingBar_SongInfoTapped;
        NowPlayingBar.TimeTapped -= NowPlayingBar_TimeTapped;
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
        if (_isDisposed) return;
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
}