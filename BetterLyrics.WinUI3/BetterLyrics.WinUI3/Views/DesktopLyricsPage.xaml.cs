using System.Diagnostics;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Rendering;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DesktopLyricsPage : Page
    {
        private readonly DispatcherQueue _dispatcherQueue = DispatcherQueue.GetForCurrentThread();

        private double _limitedLineWidth = 0;

        private readonly DesktopLyricsRenderer _lyricsRenderer =
            Ioc.Default.GetService<DesktopLyricsRenderer>()!;

        private GlobalViewModel GlobalSettingsViewModel { get; set; } =
            Ioc.Default.GetService<GlobalViewModel>()!;

        public DesktopLyricsPage()
        {
            this.InitializeComponent();

            Debug.WriteLine("hashcode for DesktopLyricsRenderer: " + _lyricsRenderer.GetHashCode());

            WeakReferenceMessenger.Default.Register<
                DesktopLyricsPage,
                DesktopLyricsRelayoutRequestedMessage
            >(
                this,
                async (r, m) =>
                {
                    await _lyricsRenderer.ReLayoutAsync(LyricsCanvas);
                }
            );

            WeakReferenceMessenger.Default.Register<DesktopLyricsPage, SongInfoChangedMessage>(
                this,
                (r, m) =>
                {
                    _dispatcherQueue.TryEnqueue(
                        DispatcherQueuePriority.High,
                        async () =>
                        {
                            _lyricsRenderer.LyricsLines = m.Value?.LyricsLines ?? [];
                            await _lyricsRenderer.ReLayoutAsync(LyricsCanvas);
                        }
                    );
                }
            );

            WeakReferenceMessenger.Default.Register<
                DesktopLyricsPage,
                PlayingPositionChangedMessage
            >(
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

        private void LyricsCanvas_Draw(
            ICanvasAnimatedControl sender,
            CanvasAnimatedDrawEventArgs args
        )
        {
            using var ds = args.DrawingSession;
            _lyricsRenderer.Draw(sender, ds);
        }

        private void LyricsCanvas_Update(
            ICanvasAnimatedControl sender,
            CanvasAnimatedUpdateEventArgs args
        )
        {
            _lyricsRenderer.AddElapsedTime(args.Timing.ElapsedTime);
            _lyricsRenderer.Calculate(sender);
        }

        private void LyricsPlaceholderGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _lyricsRenderer.LimitedLineWidth = e.NewSize.Width;
        }
    }
}
