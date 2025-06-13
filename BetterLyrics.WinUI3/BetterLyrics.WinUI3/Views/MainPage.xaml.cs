using System;
using System.Diagnostics;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Rendering;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using DispatcherQueuePriority = Microsoft.UI.Dispatching.DispatcherQueuePriority;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        private double _limitedLineWidth = 0;

        public MainViewModel ViewModel => (MainViewModel)DataContext;

        private GlobalViewModel GlobalSettingsViewModel { get; set; } =
            Ioc.Default.GetService<GlobalViewModel>()!;

        private readonly InAppLyricsRenderer _lyricsRenderer =
            Ioc.Default.GetService<InAppLyricsRenderer>()!;

        private readonly AlbumArtRenderer _albumArtRenderer =
            Ioc.Default.GetService<AlbumArtRenderer>()!;

        private readonly ILogger<MainPage> _logger = Ioc.Default.GetService<ILogger<MainPage>>()!;

        public AlbumArtViewModel AlbumArtViewModel { get; set; } =
            Ioc.Default.GetService<AlbumArtViewModel>()!;

        public MainPage()
        {
            this.InitializeComponent();

            Debug.WriteLine("hashcode for InAppLyricsRenderer: " + _lyricsRenderer.GetHashCode());

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

            WeakReferenceMessenger.Default.Register<
                MainPage,
                DesktopLyricsRelayoutRequestedMessage
            >(
                this,
                async (r, m) =>
                {
                    await _lyricsRenderer.ReLayoutAsync(LyricsCanvas);
                }
            );

            WeakReferenceMessenger.Default.Register<MainPage, SongInfoChangedMessage>(
                this,
                (r, m) =>
                {
                    _dispatcherQueue.TryEnqueue(
                        DispatcherQueuePriority.High,
                        async () =>
                        {
                            await _lyricsRenderer.ReLayoutAsync(LyricsCanvas, m.Value?.LyricsLines);
                        }
                    );
                }
            );

            WeakReferenceMessenger.Default.Register<MainPage, PlayingPositionChangedMessage>(
                this,
                (r, m) =>
                {
                    _dispatcherQueue.TryEnqueue(
                        DispatcherQueuePriority.High,
                        () => _lyricsRenderer.CurrentTime = m.Value
                    );
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
            if (
                GlobalSettingsViewModel.DisplayType == DisplayType.SplitView
                || GlobalSettingsViewModel.DisplayType == DisplayType.LyricsOnly
            )
            {
                _lyricsRenderer.Draw(sender, ds);
            }
        }

        // Comsumes CPU related resources
        private void LyricsCanvas_Update(
            ICanvasAnimatedControl sender,
            CanvasAnimatedUpdateEventArgs args
        )
        {
            _albumArtRenderer.Calculate(sender);

            _lyricsRenderer.AddElapsedTime(args.Timing.ElapsedTime);
            _lyricsRenderer.Calculate(sender);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (App.Current.SettingsWindow is null)
            {
                var settingsWindow = new HostWindow();
                settingsWindow.Navigate(typeof(SettingsPage));
                App.Current.SettingsWindow = settingsWindow;
            }

            var settingsAppWindow = App.Current.SettingsWindow.AppWindow;

            if (settingsAppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Restore();
            }

            settingsAppWindow.Show();
            settingsAppWindow.MoveInZOrderAtTop();
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
            await _lyricsRenderer.ReLayoutAsync(LyricsCanvas);
        }

        private void OpenMatchedFileButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenMatchedFileFolderInFileExplorer((string)(sender as Button)!.Tag);
        }

        private void CoverImageGrid_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateAlbumArtCornerRadius(AlbumArtViewModel.CoverImageRadius);
        }

        private void DesktopLyricsToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            if (App.Current.OverlayWindow is null)
            {
                var overlayWindow = new OverlayWindow();
                overlayWindow.Navigate(typeof(DesktopLyricsPage));
                App.Current.OverlayWindow = overlayWindow;
            }

            var overlayAppWindow = App.Current.OverlayWindow!.AppWindow;
            overlayAppWindow.Show();
        }

        private void DesktopLyricsToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            var overlayAppWindow = App.Current.OverlayWindow!.AppWindow;
            overlayAppWindow.Hide();
        }

        private async void LyricsCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            await _lyricsRenderer.ReLayoutAsync(LyricsCanvas);
        }
    }
}
