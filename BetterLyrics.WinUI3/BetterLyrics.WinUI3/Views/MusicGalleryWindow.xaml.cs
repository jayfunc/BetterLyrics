using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MusicGalleryWindow : Window,
         IRecipient<PropertyChangedMessage<BitmapDecoder?>>,
         IRecipient<PropertyChangedMessage<ElementTheme>>
    {
        public MusicGalleryWindowViewModel ViewModel { get; private set; } = Ioc.Default.GetRequiredService<MusicGalleryWindowViewModel>();

        private readonly IMediaSessionsService _mediaSessionsService = Ioc.Default.GetRequiredService<IMediaSessionsService>();

        public MusicGalleryWindow()
        {
            InitializeComponent();

            this.Init("MusicGalleryPageTitle");

            AppWindow.Closing += AppWindow_Closing;

            WeakReferenceMessenger.Default.RegisterAll(this);

            _ = UpdateAlbumArtThemeColorsAsync();
        }

        private async Task UpdateAlbumArtThemeColorsAsync()
        {
            var result = await _mediaSessionsService.CalculateAlbumArtThemeColorsAsync(
                ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus, Colors.Transparent);

            NowPlayingPage.AlbumArtThemeColors = result;
            RootGrid.RequestedTheme = result.ThemeType;
        }


        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (ViewModel.AppSettings.MusicGallerySettings.ExitOnWindowClosed)
            {
                WindowHook.ExitApp();
            }
            else
            {
                this.CloseWindow();
            }
        }

        public async void Receive(PropertyChangedMessage<BitmapDecoder?> message)
        {
            if (message.Sender is IMediaSessionsService)
            {
                if (message.PropertyName == nameof(IMediaSessionsService.AlbumArtBitmapDecoder))
                {
                    if (message.NewValue is BitmapDecoder decoder)
                    {
                        await UpdateAlbumArtThemeColorsAsync();
                    }
                }
            }
        }

        public async void Receive(PropertyChangedMessage<ElementTheme> message)
        {
            if (message.Sender == ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus.LyricsBackgroundSettings)
            {
                if (message.PropertyName == nameof(LyricsBackgroundSettings.LyricsBackgroundTheme))
                {
                    await UpdateAlbumArtThemeColorsAsync();
                }
            }
        }

        private void NowPlayingBar_SongInfoTapped(object sender, System.EventArgs e)
        {
            NowPlayingBar.ShowSongInfo = false;
            NowPlayingBar.ShowTime = true;
            NowPlayingBar.IsAutoHideEnabled = true;
            NowPlayingPage.Visibility = Visibility.Visible;
            NowPlayingPage.Opacity = 1;
        }

        private async void NowPlayingBar_TimeTapped(object sender, System.EventArgs e)
        {
            NowPlayingBar.ShowSongInfo = true;
            NowPlayingBar.ShowTime = false;
            NowPlayingBar.IsAutoHideEnabled = false;
            NowPlayingPage.Opacity = 0;
            await Task.Delay(Constants.Time.AnimationDuration);
            NowPlayingPage.Visibility = Visibility.Collapsed;
        }

        private void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus.IsOpened = true;
        }

        private void RootGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.AppSettings.MusicGallerySettings.LyricsWindowStatus.IsOpened = false;
        }
    }
}
