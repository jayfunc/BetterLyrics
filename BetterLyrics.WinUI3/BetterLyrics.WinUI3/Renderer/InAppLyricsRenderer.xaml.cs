using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Renderer
{
    public sealed partial class InAppLyricsRenderer : UserControl
    {
        public InAppLyricsRendererViewModel ViewModel =
            Ioc.Default.GetRequiredService<InAppLyricsRendererViewModel>();

        public InAppLyricsRenderer()
        {
            InitializeComponent();
        }

        private void LyricsCanvas_Draw(
            Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender,
            Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedDrawEventArgs args
        )
        {
            ViewModel.Draw(sender, args.DrawingSession);
        }

        private void LyricsCanvas_Update(
            Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender,
            Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedUpdateEventArgs args
        )
        {
            ViewModel.Calculate(sender, args);
        }
    }
}
