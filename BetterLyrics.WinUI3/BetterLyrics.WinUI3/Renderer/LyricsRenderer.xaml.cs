// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Renderer
{
    public sealed partial class LyricsRenderer : UserControl
    {
        public LyricsRendererViewModel ViewModel { get; set; }

        public LyricsRenderer()
        {
            InitializeComponent();
            ViewModel = Ioc.Default.GetRequiredService<LyricsRendererViewModel>();
        }

        private void LyricsCanvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedDrawEventArgs args)
        {
            ViewModel.Draw(sender, args.DrawingSession);
        }

        private void LyricsCanvas_Update(Microsoft.Graphics.Canvas.UI.Xaml.ICanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedUpdateEventArgs args)
        {
            ViewModel.Update(sender, args);
        }
    }
}
