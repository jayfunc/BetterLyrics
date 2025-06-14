using System;
using System.Diagnostics;
using System.Drawing;
using BetterInAppLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Rendering;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinRT;
using WinRT.Interop;
using DispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue;
using DispatcherQueuePriority = Microsoft.UI.Dispatching.DispatcherQueuePriority;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class InAppLyricsPage : Page
    {
        private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        public InAppLyricsViewModel ViewModel => (InAppLyricsViewModel)DataContext;

        private GlobalViewModel GlobalSettingsViewModel { get; set; } =
            Ioc.Default.GetService<GlobalViewModel>()!;

        private readonly InAppLyricsRenderer _lyricsRenderer =
            Ioc.Default.GetService<InAppLyricsRenderer>()!;

        private readonly AlbumArtRenderer _albumArtRenderer =
            Ioc.Default.GetService<AlbumArtRenderer>()!;

        private readonly ILogger<InAppLyricsPage> _logger = Ioc.Default.GetService<
            ILogger<InAppLyricsPage>
        >()!;

        public AlbumArtViewModel AlbumArtViewModel { get; set; } =
            Ioc.Default.GetService<AlbumArtViewModel>()!;

        public InAppLyricsPage()
        {
            this.InitializeComponent();

            Debug.WriteLine("hashcode for InAppLyricsRenderer: " + _lyricsRenderer.GetHashCode());

            DataContext = Ioc.Default.GetService<InAppLyricsViewModel>();

            // set lyrics font color

            if (GlobalSettingsViewModel.IsFirstRun)
            {
                WelcomeTeachingTip.IsOpen = true;
            }

            WeakReferenceMessenger.Default.Register<
                InAppLyricsPage,
                AlbumArtCornerRadiusChangedMessage
            >(
                this,
                (r, m) =>
                {
                    UpdateAlbumArtCornerRadius(m.Value);
                }
            );

            WeakReferenceMessenger.Default.Register<InAppLyricsPage, SongInfoChangedMessage>(
                this,
                (r, m) =>
                {
                    _dispatcherQueue.TryEnqueue(
                        DispatcherQueuePriority.High,
                        async () =>
                        {
                            await ViewModel.UpdateSongInfoUI(m.Value);
                            _lyricsRenderer.LyricsLines = m.Value?.LyricsLines ?? [];
                        }
                    );
                }
            );

            WeakReferenceMessenger.Default.Register<InAppLyricsPage, PlayingPositionChangedMessage>(
                this,
                (r, m) =>
                {
                    _dispatcherQueue.TryEnqueue(
                        DispatcherQueuePriority.High,
                        () => _lyricsRenderer.TotalTime = m.Value
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

            switch (GlobalSettingsViewModel.DisplayType)
            {
                case DisplayType.AlbumArtOnly:
                case DisplayType.PlaceholderOnly:
                    break;
                case DisplayType.LyricsOnly:
                case DisplayType.SplitView:
                    _lyricsRenderer.Draw(sender, ds);
                    break;
                default:
                    break;
            }
        }

        // Comsumes CPU related resources
        private void LyricsCanvas_Update(
            ICanvasAnimatedControl sender,
            CanvasAnimatedUpdateEventArgs args
        )
        {
            _albumArtRenderer.Calculate(sender);

            _lyricsRenderer.TotalTime += args.Timing.ElapsedTime;
            _lyricsRenderer.Calculate(sender);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = WindowHelper.CreateHostWindow();
            settingsWindow.Navigate(typeof(SettingsPage));
            settingsWindow.Activate();
            App.Current.SettingsWindow = settingsWindow;
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

        private void LyricsPlaceholderGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _lyricsRenderer.LimitedLineWidth = e.NewSize.Width;
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
            var overlayWindow = WindowHelper.CreateOverlayWindow();
            overlayWindow.Navigate(typeof(DesktopLyricsPage));
            overlayWindow.Activate();
            App.Current.OverlayWindow = overlayWindow;
        }

        private void DesktopLyricsToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            var overlayWindow = App.Current.OverlayWindow!;
            TransparentAppBarHelper.Disable(overlayWindow);
            overlayWindow.Close();
            App.Current.OverlayWindow = null;
        }
    }
}
