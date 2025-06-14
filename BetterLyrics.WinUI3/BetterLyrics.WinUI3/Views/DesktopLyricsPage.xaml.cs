using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Rendering;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Graphics.Canvas.UI.Xaml;
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

        private readonly DesktopLyricsRenderer _lyricsRenderer =
            Ioc.Default.GetService<DesktopLyricsRenderer>()!;

        private readonly DesktopBackgroundRenderer _lyricsBackgroundRenderer =
            Ioc.Default.GetService<DesktopBackgroundRenderer>()!;

        public DesktopLyricsViewModel ViewModel => (DesktopLyricsViewModel)DataContext;

        private GlobalViewModel GlobalSettingsViewModel { get; set; } =
            Ioc.Default.GetService<GlobalViewModel>()!;

        public DesktopLyricsPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetService<DesktopLyricsViewModel>();

            WeakReferenceMessenger.Default.Register<DesktopLyricsPage, SongInfoChangedMessage>(
                this,
                (r, m) =>
                {
                    _dispatcherQueue.TryEnqueue(
                        DispatcherQueuePriority.High,
                        () =>
                        {
                            _lyricsRenderer.LyricsLines = m.Value?.LyricsLines ?? [];
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
                        () => _lyricsRenderer.TotalTime = m.Value
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

            _lyricsBackgroundRenderer.Draw(sender, ds);
            _lyricsRenderer.Draw(sender, ds);
        }

        private void LyricsCanvas_Update(
            ICanvasAnimatedControl sender,
            CanvasAnimatedUpdateEventArgs args
        )
        {
            _lyricsBackgroundRenderer.ElapsedTime = args.Timing.ElapsedTime;
            _lyricsBackgroundRenderer.Calculate();

            _lyricsRenderer.TotalTime += args.Timing.ElapsedTime;
            _lyricsRenderer.Calculate(sender);
        }

        private void LyricsPlaceholderGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _lyricsRenderer.LimitedLineWidth = e.NewSize.Width;
        }
    }
}
