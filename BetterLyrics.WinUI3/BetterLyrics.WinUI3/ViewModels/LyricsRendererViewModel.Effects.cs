using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Effects;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsRendererViewModel
    {
        private OpacityEffect? _lastBgImageEffect;
        private OpacityEffect? _bgImageEffect;

        private OpacityEffect? _lastFgImageEffect;
        private OpacityEffect? _fgImageEffect;

        private OpacityEffect CreateBgImageEffect(CanvasBitmap canvasBitmap, float opacity)
        {
            float imageWidth = (float)canvasBitmap.Size.Width;
            float imageHeight = (float)canvasBitmap.Size.Height;

            float targetSize = MathF.Sqrt(MathF.Pow(_canvasWidth, 2) + MathF.Pow(_canvasHeight, 2));
            float scaleFactor = targetSize / MathF.Min(imageWidth, imageHeight);

            // Original source: https://zhuanlan.zhihu.com/p/37178216
            float gain = _lyricsBgBrightnessTransition.Value;

            float whiteX = 1 - 0.5f * gain;
            float whiteY = 0.5f + 0.5f * gain;
            float blackX = 0.5f - 0.5f * gain;
            float blackY = 0 + 0.5f * gain;

            return new OpacityEffect
            {
                Source = new BrightnessEffect
                {
                    Source = new ScaleEffect
                    {
                        Scale = new Vector2(scaleFactor),
                        Source = canvasBitmap,
                    },
                    WhitePoint = new Vector2(whiteX, whiteY),
                    BlackPoint = new Vector2(blackX, blackY),
                },
                Opacity = opacity,
            };
        }

        private OpacityEffect? CreateFgImageEffect(ICanvasAnimatedControl control, CanvasBitmap canvasBitmap, float opacity)
        {
            // TODO 最大化/还原时图片大小未跟随改变
            if (opacity == 0) return null;

            float imageWidth = (float)canvasBitmap.Size.Width;
            float imageHeight = (float)canvasBitmap.Size.Height;

            float scaleFactor = _albumArtSize / Math.Min(imageWidth, imageHeight);
            if (scaleFactor < 0.01f) return null;

            float cornerRadius = _albumArtCornerRadius / 100f * _albumArtSize / 2;

            // TODO 当前未监听专辑封面圆角变化
            var cornerRadiusMask = new CanvasCommandList(control);
            var cornerRadiusMaskDs = cornerRadiusMask.CreateDrawingSession();
            cornerRadiusMaskDs.FillRoundedRectangle(
                new Rect(0, 0, imageWidth * scaleFactor, imageHeight * scaleFactor),
                cornerRadius, cornerRadius, Colors.White
            );

            return new OpacityEffect
            {
                Source = new AlphaMaskEffect
                {
                    Source = new ScaleEffect
                    {
                        Scale = new Vector2(scaleFactor),
                        Source = canvasBitmap,
                    },
                    AlphaMask = cornerRadiusMask,
                },
                Opacity = opacity,
            };
        }
    }
}
