using BetterLyrics.WinUI3.Helper;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using System;
using System.Numerics;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.ViewModels.LyricsRendererViewModel
{
    public partial class LyricsRendererViewModel
    {
        private OpacityEffect? _lastBgImageEffect;
        private OpacityEffect? _bgImageEffect;

        private OpacityEffect? _lastFgImageEffect;
        private OpacityEffect? _fgImageEffect;

        private CanvasCommandList? _albumArtBgEffect;

        private OpacityEffect CreateBgImageEffect(CanvasBitmap canvasBitmap, double opacity)
        {
            double imageWidth = (double)canvasBitmap.Size.Width;
            double imageHeight = (double)canvasBitmap.Size.Height;

            double targetSize = Math.Sqrt(Math.Pow(_canvasWidth, 2) + Math.Pow(_canvasHeight, 2));
            double scaleFactor = targetSize / Math.Min(imageWidth, imageHeight);

            // Original source: https://zhuanlan.zhihu.com/p/37178216
            double gain = _lyricsBgBrightnessTransition.Value;

            double whiteX = 1 - 0.5f * gain;
            double whiteY = 0.5f + 0.5f * gain;
            double blackX = 0.5f - 0.5f * gain;
            double blackY = 0 + 0.5f * gain;

            return new OpacityEffect
            {
                Source = new BrightnessEffect
                {
                    Source = new ScaleEffect
                    {
                        Scale = new Vector2((float)scaleFactor),
                        Source = canvasBitmap,
                    },
                    WhitePoint = new Vector2((float)whiteX, (float)whiteY),
                    BlackPoint = new Vector2((float)blackX, (float)blackY),
                },
                Opacity = (float)opacity,
            };
        }

        private OpacityEffect? CreateFgImageEffect(ICanvasAnimatedControl control, CanvasBitmap canvasBitmap, double opacity)
        {
            // TODO 最大化/还原时图片大小未跟随改变
            if (opacity == 0) return null;

            double imageWidth = (double)canvasBitmap.Size.Width;
            double imageHeight = (double)canvasBitmap.Size.Height;

            double scaleFactor = _albumArtSize / Math.Min(imageWidth, imageHeight);
            if (scaleFactor < 0.01f) return null;

            double cornerRadius = _albumArtCornerRadius / 100f * _albumArtSize / 2;

            var cornerRadiusMask = new CanvasCommandList(control);
            using var cornerRadiusMaskDs = cornerRadiusMask.CreateDrawingSession();
            cornerRadiusMaskDs.FillRoundedRectangle(
                new Rect(0, 0, imageWidth * scaleFactor, imageHeight * scaleFactor),
                (float)cornerRadius, (float)cornerRadius, Colors.White
            );

            return new OpacityEffect
            {
                Source = new AlphaMaskEffect
                {
                    Source = new ScaleEffect
                    {
                        Scale = new Vector2((float)scaleFactor),
                        Source = canvasBitmap,
                    },
                    AlphaMask = cornerRadiusMask,
                },
                Opacity = (float)opacity,
            };
        }

        private void UpdateAlbumArtBgEffect(ICanvasAnimatedControl control)
        {
            _albumArtBgEffect?.Dispose();
            _albumArtBgEffect = null;

            using var overlappedCovers = new CanvasCommandList(control);
            using var overlappedCoversDs = overlappedCovers.CreateDrawingSession();

            if (_lastBgImageEffect != null && !_lastBgImageEffect.IsDisposed() && _lastAlbumArtCanvasBitmap != null)
            {
                DrawBackgroundImgae(_lastBgImageEffect, overlappedCoversDs, _lastAlbumArtCanvasBitmap);
            }
            if (_bgImageEffect != null && !_bgImageEffect.IsDisposed() && _albumArtCanvasBitmap != null)
            {
                DrawBackgroundImgae(_bgImageEffect, overlappedCoversDs, _albumArtCanvasBitmap);
            }

            using var blurredCover = new GaussianBlurEffect
            {
                BlurAmount = _albumArtBgBlurAmount,
                Source = overlappedCovers,
                BorderMode = EffectBorderMode.Soft,
                Optimization = EffectOptimization.Speed,
            };

            using var combined = new CanvasCommandList(control);
            using var combinedDs = combined.CreateDrawingSession();

            if (_coverAcrylicEffectAmount > 0 && _coverAcrylicNoiseCanvasBitmap != null)
            {
                // 应用亚克力噪点效果
                combinedDs.DrawImage(new BlendEffect
                {
                    Mode = BlendEffectMode.SoftLight,
                    Background = blurredCover,
                    Foreground = new OpacityEffect
                    {
                        Source = _coverAcrylicNoiseCanvasBitmap,
                        Opacity = _coverAcrylicEffectAmount / 100f,
                    },
                });
            }
            else
            {
                combinedDs.DrawImage(blurredCover);
            }

            _albumArtBgEffect = new CanvasCommandList(control);
            using var albumArtBgDs = _albumArtBgEffect.CreateDrawingSession();
            albumArtBgDs.DrawImage(new OpacityEffect
            {
                Opacity = _albumArtBgOpacity / 100f,
                Source = combined,
            });
        }

        private void UpdateLastBgImageEffect()
        {
            _lastBgImageEffect?.Dispose();
            _lastBgImageEffect = null;
            if (_lastAlbumArtCanvasBitmap != null)
            {
                _lastBgImageEffect = CreateBgImageEffect(_lastAlbumArtCanvasBitmap, 1 - _albumArtBgTransition.Value);
            }
        }

        private void UpdateBgImageEffect()
        {
            _bgImageEffect?.Dispose();
            _bgImageEffect = null;
            if (_albumArtCanvasBitmap != null)
            {
                _bgImageEffect = CreateBgImageEffect(_albumArtCanvasBitmap, _albumArtBgTransition.Value);
            }
        }

        private void UpdateLastFgImageEffect(ICanvasAnimatedControl control)
        {
            _lastFgImageEffect?.Dispose();
            _lastFgImageEffect = null;
            if (_lastAlbumArtCanvasBitmap != null)
            {
                _lastFgImageEffect = CreateFgImageEffect(control, _lastAlbumArtCanvasBitmap, 1 - _albumArtBgTransition.Value);
            }
        }

        private void UpdateFgImageEffect(ICanvasAnimatedControl control)
        {
            _fgImageEffect?.Dispose();
            _fgImageEffect = null;
            if (_albumArtCanvasBitmap != null)
            {
                _fgImageEffect = CreateFgImageEffect(control, _albumArtCanvasBitmap, _albumArtBgTransition.Value);
            }
        }
    }
}
