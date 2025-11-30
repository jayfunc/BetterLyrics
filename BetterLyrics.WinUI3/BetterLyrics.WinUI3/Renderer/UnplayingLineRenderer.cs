using BetterLyrics.WinUI3.Extensions;
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

            if (line.PhoneticCanvasTextLayout != null)
            {
                var opacity = line.UnplayingOpacityTransition.Value;
                DrawPart(ds, textOnlyLayer,
                    line.PhoneticCanvasTextLayout,
                    line.PhoneticPosition,
                    blurAmount,
                    (float)opacity);
            }

            if (line.OriginalCanvasTextLayout != null)
            {
                double opacity;
                if (line.PlayingOpacityTransition.StartValue > line.UnplayingOpacityTransition.StartValue)
                {
                    opacity = line.PlayingOpacityTransition.Value;
                }
                else
                {
                    opacity = line.UnplayingOpacityTransition.Value;
                }
                DrawPart(ds, textOnlyLayer,
                    line.OriginalCanvasTextLayout,
                    line.OriginalPosition,
                    blurAmount,
                    (float)opacity);
            }

            if (line.TranslatedCanvasTextLayout != null)
            {
                var opacity = line.UnplayingOpacityTransition.Value;
                DrawPart(ds, textOnlyLayer,
                    line.TranslatedCanvasTextLayout,
                    line.TranslatedPosition,
                    blurAmount,
                    (float)opacity);
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


            ds.DrawImage(new OpacityEffect
            {
                Source = new GaussianBlurEffect
                {
                    BlurAmount = blur,
                    Source = new CropEffect
                    {
                        Source = source,
                        SourceRectangle = destRect,
                        BorderMode = EffectBorderMode.Hard,
                    },
                    BorderMode = EffectBorderMode.Soft
                },
                Opacity = opacity
            });
        }
    }
}
