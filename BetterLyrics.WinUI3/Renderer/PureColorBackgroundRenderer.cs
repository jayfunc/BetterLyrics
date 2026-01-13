using BetterLyrics.WinUI3.Extensions;
using Microsoft.Graphics.Canvas;
using Windows.Foundation;
using Windows.UI;

namespace BetterLyrics.WinUI3.Renderer
{
    public class PureColorBackgroundRenderer
    {
        public void Draw(
            CanvasDrawingSession ds,
            Rect bounds,
            Color color,
            double opacity,
            bool isEnabled)
        {
            if (!isEnabled || opacity <= 0) return;

            ds.FillRectangle(
                bounds,
                color.WithAlpha((byte)(opacity * 255))
            );
        }
    }
}