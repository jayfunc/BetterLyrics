using System.Diagnostics;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Renderer
{
    public sealed partial class InAppLyricsRenderer : UserControl
    {
        public InAppLyricsRendererViewModel ViewModel { get; set; }

        public InAppLyricsRenderer()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<InAppLyricsRendererViewModel>();
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

        private void LyricsCanvas_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.RequestRelayout();
        }
    }
}
