using System;
using Windows.Foundation;
using Windows.Graphics.DirectX;
using Windows.UI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace BetterLyrics.WinUI3.Renderer;

public partial class CompositionRenderer : IDisposable
{
    private CanvasRenderTarget? _renderTarget;

    public void Dispose()
    {
        _renderTarget?.Dispose();
        _renderTarget = null;
    }

    public CanvasRenderTarget Render(
        ICanvasAnimatedControl sender,
        Size size,
        float dpi,
        Color clearColor,
        Action<CanvasDrawingSession> drawCommands)
    {
        var width = (float)size.Width;
        var height = (float)size.Height;

        float widthInPixels = sender.ConvertDipsToPixels(width, CanvasDpiRounding.Round);
        float heightInPixels = sender.ConvertDipsToPixels(height, CanvasDpiRounding.Round);

        // 如果尺寸或 DPI 发生变化，或者目标被意外销毁，则重新创建
        if (_renderTarget == null ||
            _renderTarget.SizeInPixels.Width != widthInPixels ||
            _renderTarget.SizeInPixels.Height != heightInPixels)
        {
            _renderTarget?.Dispose();
            _renderTarget = new CanvasRenderTarget(sender, width, height, dpi,
                DirectXPixelFormat.B8G8R8A8UIntNormalized, CanvasAlphaMode.Premultiplied);
        }

        using (var ds = _renderTarget.CreateDrawingSession())
        {
            ds.Clear(clearColor);

            drawCommands?.Invoke(ds);
        }

        return _renderTarget;
    }
}