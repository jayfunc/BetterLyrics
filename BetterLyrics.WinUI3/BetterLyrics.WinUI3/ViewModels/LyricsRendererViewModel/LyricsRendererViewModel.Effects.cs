using BetterLyrics.WinUI3.Helper;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using System;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace BetterLyrics.WinUI3.ViewModels.LyricsRendererViewModel
{
    public partial class LyricsRendererViewModel
    {
        private OpacityEffect? _albumArtBgEffect;
        private CanvasCommandList? _albumArtEffect;
        private PixelShaderEffect? _fluidEffect;

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
            if (opacity == 0) return null;

            double imageWidth = (double)canvasBitmap.Size.Width;
            double imageHeight = (double)canvasBitmap.Size.Height;

            double scaleFactor = _albumArtSize / Math.Min(imageWidth, imageHeight);
            if (scaleFactor < 0.01f) return null;

            double cornerRadius = _liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.CoverImageRadius / 100f * _albumArtSize / 2;

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

        private void DisposeAlbumArtBgEffect()
        {
            _albumArtBgEffect?.Dispose();
            _albumArtBgEffect = null;
        }

        /// <summary>
        /// 更新专辑封面背景效果
        /// <para>应该在以下任意条件满足时调用此函数：</para>
        /// <para><seealso cref="_isCanvasWidthChanged"/> == true</para>
        /// <para><seealso cref="_isCanvasHeightChanged"/> == true</para>
        /// <para><seealso cref="_albumArtChanged"/> == true</para>
        /// <para><seealso cref="_isAlbumArtBgBlurAmountChanged"/> == true</para>
        /// <para><seealso cref="_isCoverAcrylicEffectAmountChanged"/> == true</para>
        /// <para><seealso cref="_isAlbumArtBgOpacityChanged"/> == true</para>
        /// <para><seealso cref="_albumArtBgTransition"/> 正在变化</para>
        /// <para><seealso cref="_albumArtAccentColor1Transition"/> 正在变化</para>
        /// 如果上述条件均不满足，需调用 <seealso cref="UpdateAlbumArtBgRenderTarget"/> 来更新渲染缓存
        /// </summary>
        /// <param name="control"></param>
        private void UpdateAlbumArtBgEffect(ICanvasAnimatedControl control)
        {
            DisposeAlbumArtBgEffect();

            using var overlappedCovers = new CanvasCommandList(control);
            using var overlappedCoversDs = overlappedCovers.CreateDrawingSession();

            if (_lastAlbumArtCanvasBitmap != null)
            {
                using var lastBgImageEffect = CreateBgImageEffect(_lastAlbumArtCanvasBitmap, 1 - _albumArtBgTransition.Value);
                DrawBackgroundImgae(lastBgImageEffect, overlappedCoversDs, _lastAlbumArtCanvasBitmap);
            }
            if (_albumArtCanvasBitmap != null)
            {
                using var bgImageEffect = CreateBgImageEffect(_albumArtCanvasBitmap, _albumArtBgTransition.Value);
                DrawBackgroundImgae(bgImageEffect, overlappedCoversDs, _albumArtCanvasBitmap);
            }

            using var blurredCover = new GaussianBlurEffect
            {
                BlurAmount = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.CoverOverlayBlurAmount,
                Source = overlappedCovers,
                BorderMode = EffectBorderMode.Soft,
                Optimization = EffectOptimization.Speed,
            };

            var combined = new CanvasCommandList(control);
            using var combinedDs = combined.CreateDrawingSession();

            if (_liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.CoverAcrylicEffectAmount > 0 && _coverAcrylicNoiseCanvasBitmap != null)
            {
                // 应用亚克力噪点效果
                combinedDs.DrawImage(new BlendEffect
                {
                    Mode = BlendEffectMode.SoftLight,
                    Background = blurredCover,
                    Foreground = new OpacityEffect
                    {
                        Source = _coverAcrylicNoiseCanvasBitmap,
                        Opacity = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.CoverAcrylicEffectAmount / 100f,
                    },
                });
            }
            else
            {
                combinedDs.DrawImage(blurredCover);
            }

            _albumArtBgEffect = new OpacityEffect
            {
                Opacity = _liveStatesService.LiveStates.LyricsWindowStatus.LyricsBackgroundSettings.CoverOverlayOpacity / 100f,
                Source = combined,
            };
        }

        private void DisposeAlbumArtBgRenderTarget()
        {
            _albumArtBgRenderTarget?.Dispose();
            _albumArtBgRenderTarget = null;
        }

        private void UpdateAlbumArtBgRenderTarget(ICanvasAnimatedControl control)
        {
            DisposeAlbumArtBgRenderTarget();

            double targetSize = Math.Sqrt(Math.Pow(_canvasWidth, 2) + Math.Pow(_canvasHeight, 2));

            _albumArtBgRenderTarget = new CanvasRenderTarget(control, (float)targetSize, (float)targetSize);
            using var ds = _albumArtBgRenderTarget.CreateDrawingSession();

            float offsetX = -(float)(_canvasWidth - targetSize) / 2;
            float offsetY = -(float)(_canvasHeight - targetSize) / 2;

            ds.DrawImage(_albumArtBgEffect, new Vector2(offsetX, offsetY));
        }

        private void DisposeAlbumArtEffect()
        {
            _albumArtEffect?.Dispose();
            _albumArtEffect = null;
        }

        /// <summary>
        /// 更新专辑封面效果
        /// <para>应该在以下任意条件满足时调用此函数：</para>
        /// <para><seealso cref="_isCanvasWidthChanged"/> == true</para>
        /// <para><seealso cref="_isCanvasHeightChanged"/> == true</para>
        /// <para><seealso cref="_albumArtChanged"/> == true</para>
        /// <para><seealso cref="_isAlbumArtShadowAmountChanged"/> == true</para>
        /// <para><seealso cref="_albumArtBgTransition"/> 正在变化</para>
        /// <para><seealso cref="_albumArtAccentColor1Transition"/> 正在变化</para>
        /// 如果上述条件均不满足，需调用 <seealso cref="UpdateAlbumArtRenderTarget"/> 来更新渲染缓存
        /// </summary>
        /// <param name="control"></param>
        private void UpdateAlbumArtEffect(ICanvasAnimatedControl control)
        {
            DisposeAlbumArtEffect();

            using var overlappedCovers = new CanvasCommandList(control);
            using var overlappedCoversDs = overlappedCovers.CreateDrawingSession();

            if (_lastAlbumArtCanvasBitmap != null)
            {
                using var lastFgImageEffect = CreateFgImageEffect(control, _lastAlbumArtCanvasBitmap, 1 - _albumArtBgTransition.Value);
                if (lastFgImageEffect != null)
                {
                    overlappedCoversDs.DrawImage(lastFgImageEffect);
                }
            }
            if (_albumArtCanvasBitmap != null)
            {
                using var fgImageEffect = CreateFgImageEffect(control, _albumArtCanvasBitmap, _albumArtBgTransition.Value);
                if (fgImageEffect != null)
                {
                    overlappedCoversDs.DrawImage(fgImageEffect);
                }
            }

            _albumArtEffect = new CanvasCommandList(control);
            using var combinedDs = _albumArtEffect.CreateDrawingSession();
            combinedDs.DrawImage(new ShadowEffect
            {
                Source = overlappedCovers,
                ShadowColor = _albumArtAccentColor1Transition.Value,
                BlurAmount = _liveStatesService.LiveStates.LyricsWindowStatus.AlbumArtLayoutSettings.CoverImageShadowAmount,
                Optimization = EffectOptimization.Speed,
            });
            combinedDs.DrawImage(overlappedCovers);
        }

        private void DisposeAlbumArtRenderTarget()
        {
            _albumArtRenderTarget?.Dispose();
            _albumArtRenderTarget = null;
        }

        private void UpdateAlbumArtRenderTarget(ICanvasAnimatedControl control)
        {
            DisposeAlbumArtRenderTarget();

            _albumArtRenderTarget = new CanvasRenderTarget(control, (float)_canvasWidth, (float)_canvasHeight);
            using var ds = _albumArtRenderTarget.CreateDrawingSession();

            // 给一个偏移，是为了避免绘制时从原点开始，这样会造成阴影被裁切
            ds.DrawImage(_albumArtEffect, control.Size.ToVector2() / 2 - new Vector2((float)_albumArtSize, (float)_albumArtSize) / 2);
        }

        private void DisposeFluidEffect()
        {
            _fluidEffect?.Dispose();
            _fluidEffect = null;
        }

        private async void UpdateFluidEffect(ICanvasAnimatedControl control)
        {
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/FluidEffect.bin"));
            IBuffer buffer = await FileIO.ReadBufferAsync(file);
            var bytes = buffer.ToArray();
            _fluidEffect = new PixelShaderEffect(bytes);
            _fluidEffect.Properties["Width"] = (float)control.ConvertDipsToPixels((float)control.Size.Width, CanvasDpiRounding.Round);
            _fluidEffect.Properties["Height"] = (float)control.ConvertDipsToPixels((float)control.Size.Height, CanvasDpiRounding.Round);
            _fluidEffect.Properties["color1"] = _albumArtAccentColor1Transition.Value.ToVector3RGB();
            _fluidEffect.Properties["color2"] = _albumArtAccentColor2Transition.Value.ToVector3RGB();
            _fluidEffect.Properties["color3"] = _albumArtAccentColor3Transition.Value.ToVector3RGB();
            _fluidEffect.Properties["color4"] = _albumArtAccentColor4Transition.Value.ToVector3RGB();
            _fluidEffect.Properties["EnableLightWave"] = false;
        }
    }
}
