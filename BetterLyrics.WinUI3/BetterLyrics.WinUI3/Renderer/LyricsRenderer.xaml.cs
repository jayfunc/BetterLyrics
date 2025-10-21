// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.ViewModels.LyricsRendererViewModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Storage.Streams;

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

        private void LyricsCanvas_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            LyricsCanvas.RemoveFromVisualTree();
            LyricsCanvas = null;
        }

        private async void LyricsCanvas_CreateResources(Microsoft.Graphics.Canvas.UI.Xaml.CanvasAnimatedControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
        {
            await ViewModel.CreateResourcesAsync(sender);
        }
    }
}
