using System;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Rendering;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainViewModel ViewModel => (MainViewModel)DataContext;

        private GlobalViewModel GlobalSettingsViewModel { get; set; } =
            Ioc.Default.GetService<GlobalViewModel>()!;

        private readonly LyricsRenderer _lyricsRenderer = Ioc.Default.GetService<LyricsRenderer>()!;

        private readonly AlbumArtRenderer _albumArtRenderer =
            Ioc.Default.GetService<AlbumArtRenderer>()!;

        private readonly ILogger<MainPage> _logger = Ioc.Default.GetService<ILogger<MainPage>>()!;

        public AlbumArtViewModel AlbumArtViewModel { get; set; } =
            Ioc.Default.GetService<AlbumArtViewModel>()!;

        public MainPage()
        {
            this.InitializeComponent();

            DataContext = Ioc.Default.GetService<MainViewModel>();

            // set lyrics font color

            if (GlobalSettingsViewModel.IsFirstRun)
            {
                WelcomeTeachingTip.IsOpen = true;
            }

            WeakReferenceMessenger.Default.Register<MainPage, AlbumArtCornerRadiusChangedMessage>(
                this,
                (r, m) =>
                {
                    UpdateAlbumArtCornerRadius(m.Value);
                }
            );
        }

        private void UpdateAlbumArtCornerRadius(int cornerRadius)
        {
            if (CoverImageGrid.ActualHeight == double.NaN)
                return;

            CoverImageGrid.CornerRadius = new CornerRadius(
                cornerRadius / 100f * CoverImageGrid.ActualHeight / 2
            );
        }

        // Comsumes GPU related resources
        private void LyricsCanvas_Draw(
            ICanvasAnimatedControl sender,
            CanvasAnimatedDrawEventArgs args
        )
        {
            using var ds = args.DrawingSession;

            _albumArtRenderer.Draw(sender, ds);
            _lyricsRenderer.Draw(sender, ds);
        }

        // Comsumes CPU related resources
        private void LyricsCanvas_Update(
            ICanvasAnimatedControl sender,
            CanvasAnimatedUpdateEventArgs args
        )
        {
            _lyricsRenderer.AddElapsedTime(args.Timing.ElapsedTime);

            _albumArtRenderer.Calculate(sender);
            _lyricsRenderer.CalculateAsync();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.Current.SettingsWindow is null)
            {
                var settingsWindow = new HostWindow();
                settingsWindow.Navigate(typeof(SettingsPage));
                App.Current.SettingsWindow = settingsWindow;
            }

            var appWindow = App.Current.SettingsWindow.AppWindow;

            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Restore();
            }

            appWindow.Show();
            appWindow.MoveInZOrderAtTop();
        }

        private void WelcomeTeachingTip_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
        {
            GlobalSettingsViewModel.IsFirstRun = false;
        }

        private void CoverArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CoverImageGrid.Width = CoverImageGrid.Height = Math.Min(
                CoverArea.ActualWidth,
                CoverArea.ActualHeight
            );
        }

        private void BottomCommandGrid_PointerEntered(
            object sender,
            Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e
        )
        {
            if (ViewModel.IsImmersiveMode && BottomCommandGrid.Opacity == 0)
                BottomCommandGrid.Opacity = .5;
        }

        private void BottomCommandGrid_PointerExited(
            object sender,
            Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e
        )
        {
            if (ViewModel.IsImmersiveMode && BottomCommandGrid.Opacity == .5)
                BottomCommandGrid.Opacity = 0;
        }

        private async void LyricsPlaceholderGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _lyricsRenderer.LimitedLineWidth = e.NewSize.Width;
            await _lyricsRenderer.ReLayoutAsync();
        }

        private async void LyricsCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _lyricsRenderer.CanvasWidth = e.NewSize.Width;
            _lyricsRenderer.CanvasHeight = e.NewSize.Height;
            await _lyricsRenderer.ReLayoutAsync();
        }

        private void OpenMatchedFileButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenMatchedFileFolderInFileExplorer((string)(sender as Button)!.Tag);
        }

        private void LyricsCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            _lyricsRenderer.Control = (ICanvasAnimatedControl)sender;
        }

        private void CoverImageGrid_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateAlbumArtCornerRadius(AlbumArtViewModel.CoverImageRadius);
        }
    }
}
