using BetterLyrics.WinUI3.Renderer;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class LyricsPreviewCanvas : UserControl
    {
        private readonly LyricsRenderer _lyricsRenderer = new();

        public LyricsPreviewCanvas()
        {
            InitializeComponent();
        }

        private void Canvas_Draw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
        }
    }
}
