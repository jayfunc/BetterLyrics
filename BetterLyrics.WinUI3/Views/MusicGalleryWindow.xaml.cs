using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Services.GSMTCService;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MusicGalleryWindow : Window,
         IRecipient<PropertyChangedMessage<BitmapImage?>>,
         IRecipient<PropertyChangedMessage<ElementTheme>>,
         IRecipient<PropertyChangedMessage<PaletteGeneratorType>>
    {
        public MusicGalleryWindowViewModel ViewModel { get; private set; }

        private readonly IGSMTCService _gsmtcService = Ioc.Default.GetRequiredService<IGSMTCService>();

        public MusicGalleryWindow()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<MusicGalleryWindowViewModel>();
            this.Init("MusicGalleryPageTitle");

            NowPlayingPage.LyricsWindowStatus = ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus;

            AppWindow.Closing += AppWindow_Closing;

            WeakReferenceMessenger.Default.RegisterAll(this);

            _ = UpdateAlbumArtThemeColorsAsync();
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
            var result = await _gsmtcService.CalculateAlbumArtThemeColorsAsync(
                ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus, Colors.Transparent);

            NowPlayingPage.LyricsWindowStatus?.WindowPalette = result;
            NowPlayingPage.RequestedTheme = result.ThemeType.ToElementTheme();

            UpdateTheme();
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (ViewModel.AppSettings.MusicGallerySettings.ExitOnWindowClosed)
            {
                WindowHook.ExitApp();
            }
            else
            {
                this.PrepareWindowClosing();
            }
        }

        private void NowPlayingBar_SongInfoTapped(object sender, System.EventArgs e)
        {
            NowPlayingBar.ShowSongInfo = false;
            NowPlayingBar.ShowTime = true;
            NowPlayingBar.IsAutoHideEnabled = true;
            NowPlayingPage.Visibility = Visibility.Visible;
            NowPlayingPage.Opacity = 1;
            UpdateTheme();
        }

        private async void NowPlayingBar_TimeTapped(object sender, System.EventArgs e)
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

        private void NowPlayingBar_PlayingQueueClick(object sender, System.EventArgs e)
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

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            this.Closed -= Window_Closed;

            WeakReferenceMessenger.Default.UnregisterAll(this);

            this.AppWindow.Closing -= AppWindow_Closing;
        }

        public void Receive(PropertyChangedMessage<BitmapImage?> message)
        {
            if (message.Sender is IGSMTCService)
            {
                if (message.PropertyName == nameof(IGSMTCService.AlbumArtBitmapImage))
                {
                    _ = UpdateAlbumArtThemeColorsAsync();
                }
            }
        }

        public void Receive(PropertyChangedMessage<ElementTheme> message)
        {
            if (message.Sender == ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.WindowTheme))
                {
                    _ = UpdateAlbumArtThemeColorsAsync();
                }
            }
            else if (message.Sender is GeneralSettings)
            {
                if (message.PropertyName == nameof(GeneralSettings.AppTheme))
                {
                    UpdateTheme();
                }
            }
        }

        public void Receive(PropertyChangedMessage<PaletteGeneratorType> message)
        {
            if (message.Sender == ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.PaletteGeneratorType))
                {
                    _ = UpdateAlbumArtThemeColorsAsync();
                }
            }
        }

    }
}
