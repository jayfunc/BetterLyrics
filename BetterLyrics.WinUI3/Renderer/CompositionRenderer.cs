using System;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Windows.Foundation;
using Windows.UI;

namespace BetterLyrics.WinUI3.Renderer
{
    public partial class CompositionRenderer : IDisposable
    {
        private CanvasRenderTarget? _renderTarget;

        public CanvasRenderTarget Render(
            ICanvasAnimatedControl sender,
            Size size,
            float dpi,
            Color clearColor,
            Action<CanvasDrawingSession> drawCommands)
        {
            float width = (float)size.Width;
            float height = (float)size.Height;

            float widthInPixels = sender.ConvertDipsToPixels(width, CanvasDpiRounding.Round);
            float heightInPixels = sender.ConvertDipsToPixels(height, CanvasDpiRounding.Round);

            // 如果尺寸或 DPI 发生变化，或者目标被意外销毁，则重新创建
            if (_renderTarget == null ||
                _renderTarget.SizeInPixels.Width != widthInPixels ||
                _renderTarget.SizeInPixels.Height != heightInPixels)
            {
                _renderTarget?.Dispose();
                _renderTarget = new CanvasRenderTarget(sender, width, height, dpi);
            }

            using (var ds = _renderTarget.CreateDrawingSession())
            {
                ds.Clear(clearColor);

                drawCommands?.Invoke(ds);
            }

            return _renderTarget;
        }

        public void Dispose()
        {
            _renderTarget?.Dispose();
            _renderTarget = null;
        }
    }
}
