using BetterLyrics.WinUI3.Models;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using System.Numerics;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Renderer
{
    public class UnplayingLineRenderer
    {
        public void Draw(
            CanvasDrawingSession ds,
            ICanvasImage textOnlyLayer,
            LyricsLine line)
        {
            var blurAmount = (float)line.BlurAmountTransition.Value;
            var opacity = line.OpacityTransition.Value;

            if (line.PhoneticCanvasTextLayout != null)
            {
                DrawPart(ds, textOnlyLayer,
                    line.PhoneticCanvasTextLayout,
                    line.PhoneticPosition,
                    blurAmount,
                    (float)opacity * 0.3f);
            }

            if (line.OriginalCanvasTextLayout != null)
            {
                DrawPart(ds, textOnlyLayer,
                    line.OriginalCanvasTextLayout,
                    line.OriginalPosition,
                    blurAmount,
                    (float)opacity);
            }

            if (line.TranslatedCanvasTextLayout != null)
            {
                DrawPart(ds, textOnlyLayer,
                    line.TranslatedCanvasTextLayout,
                    line.TranslatedPosition,
                    blurAmount,
                    (float)opacity * 0.3f);
            }
        }

        private void DrawPart(
            CanvasDrawingSession ds,
            ICanvasImage source,
            CanvasTextLayout layout,
            Vector2 position,
            float blur,
            float opacity)
        {
            if (opacity <= 0) return;

            var bounds = layout.LayoutBounds;
            var destRect = new Rect(
                bounds.X + position.X,
                bounds.Y + position.Y,
                bounds.Width,
                bounds.Height
            );

            using (var blurEffect = new GaussianBlurEffect
            {
                BlurAmount = blur,
                Source = source,
                BorderMode = EffectBorderMode.Soft
            })
            {
                ds.DrawImage(blurEffect, destRect, destRect, opacity);
            }
        }
    }
}
