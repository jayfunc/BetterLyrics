using BetterLyrics.WinUI3.Models.Lyrics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Text;
using System;
using System.Numerics;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Renderer
{
    public class UnplayingLineRenderer
    {
        public void Draw(
            CanvasDrawingSession ds,
            ICanvasImage textOnlyLayer,
            RenderLyricsLine line)
        {
            var blurAmount = (float)line.BlurAmountTransition.Value;

            if (line.TertiaryTextLayout != null)
            {
                var opacity = line.PhoneticOpacityTransition.Value;
                DrawPart(ds, textOnlyLayer,
                    line.TertiaryTextLayout,
                    line.TertiaryPosition,
                    blurAmount,
                    (float)opacity);
            }

            if (line.PrimaryTextLayout != null)
            {
                double opacity = Math.Max(line.PlayedPrimaryOpacityTransition.Value, line.UnplayedPrimaryOpacityTransition.Value);
                DrawPart(ds, textOnlyLayer,
                    line.PrimaryTextLayout,
                    line.PrimaryPosition,
                    blurAmount,
                    (float)opacity);
            }

            if (line.SecondaryTextLayout != null)
            {
                var opacity = line.TranslatedOpacityTransition.Value;
                DrawPart(ds, textOnlyLayer,
                    line.SecondaryTextLayout,
                    line.SecondaryPosition,
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
            if (float.IsNaN(opacity) || opacity <= 0) return;

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
