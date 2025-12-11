using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Numerics;

namespace BetterLyrics.WinUI3.Renderer
{
    public partial class CoverBackgroundRenderer : IDisposable
    {
        private CanvasBitmap? _currentBitmap;
        private CanvasBitmap? _previousBitmap;

        private readonly ValueTransition<double> _crossfadeTransition;

        private float _rotationAngle = 0f;

        public bool IsEnabled { get; set; } = false;

        public int Opacity { get; set; } = 100;

        public int BlurAmount { get; set; } = 100;

        public int Speed { get; set; } = 100;

        public CoverBackgroundRenderer()
        {
            _crossfadeTransition = new ValueTransition<double>(1.0, 0.7, easingType: EasingType.Linear);
        }

        public void SetCoverBitmap(CanvasBitmap? newBitmap)
        {
            if (_currentBitmap == newBitmap) return;

            if (_currentBitmap == null)
            {
                _currentBitmap = newBitmap;
                _crossfadeTransition.StartTransition(1.0, jumpTo: true);
                return;
            }

            _previousBitmap = _currentBitmap;
            _currentBitmap = newBitmap;

            if (newBitmap != null)
            {
                _crossfadeTransition.Reset(0.0);
                _crossfadeTransition.StartTransition(1.0);
            }
            else
            {
                _previousBitmap = null;
                _crossfadeTransition.StartTransition(1.0, jumpTo: true);
            }
        }

        public void Update(TimeSpan deltaTime)
        {
            if (!IsEnabled) return;

            _crossfadeTransition.Update(deltaTime);

            if (Speed > 0)
            {
                float baseSpeed = 0.6f; // 弧度/秒
                float currentSpeed = (Speed / 100.0f) * baseSpeed;

                _rotationAngle += currentSpeed * (float)deltaTime.TotalSeconds;

                _rotationAngle %= (float)(2 * Math.PI);
            }

            if (_crossfadeTransition.Value >= 1.0 && _previousBitmap != null)
            {
                _previousBitmap = null;
            }
        }

        public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (!IsEnabled || Opacity <= 0) return;

            float baseAlpha = Opacity / 100.0f;
            float currentBlur = BlurAmount;

            float angle = Speed > 0 ? _rotationAngle : 0f;

            double fadeProgress = _crossfadeTransition.Value;
            bool isCrossfading = fadeProgress < 1.0 && _previousBitmap != null;

            if (isCrossfading)
            {
                DrawLayer(ds, control.Size, _previousBitmap, angle, currentBlur, baseAlpha);

                float newLayerAlpha = baseAlpha * (float)fadeProgress;
                if (newLayerAlpha > 0.005f)
                {
                    DrawLayer(ds, control.Size, _currentBitmap, angle, currentBlur, newLayerAlpha);
                }
            }
            else if (_currentBitmap != null)
            {
                DrawLayer(ds, control.Size, _currentBitmap, angle, currentBlur, baseAlpha);
            }
        }

        private void DrawLayer(CanvasDrawingSession ds, Windows.Foundation.Size screenSize, CanvasBitmap? bitmap, float rotationRadians, float blurAmount, float alpha)
        {
            if (bitmap == null) return;

            float imgW = bitmap.SizeInPixels.Width;
            float imgH = bitmap.SizeInPixels.Height;
            Vector2 screenCenter = new Vector2((float)screenSize.Width / 2f, (float)screenSize.Height / 2f);

            float scale;
            if (Speed > 0 && Math.Abs(rotationRadians) > 0.001f)
            {
                float screenDiagonal = (float)Math.Sqrt(screenSize.Width * screenSize.Width + screenSize.Height * screenSize.Height);

                float scaleX = screenDiagonal / imgW;
                float scaleY = screenDiagonal / imgH;
                scale = Math.Max(scaleX, scaleY);
            }
            else
            {
                float scaleX = (float)screenSize.Width / imgW;
                float scaleY = (float)screenSize.Height / imgH;
                scale = Math.Max(scaleX, scaleY);
            }

            // 缩放图片 -> 将图片中心移动到 (0,0) 以便旋转 -> 旋转 ->将图片移回屏幕中心
            Vector2 imgCenterOffset = new Vector2(
                ((float)screenSize.Width - imgW * scale) / 2.0f,
                ((float)screenSize.Height - imgH * scale) / 2.0f
            );

            Matrix3x2 transform =
                Matrix3x2.CreateScale(scale) * Matrix3x2.CreateTranslation(imgCenterOffset) * Matrix3x2.CreateRotation(rotationRadians, screenCenter);

            using (var transformEffect = new Transform2DEffect())
            using (var blurEffect = new GaussianBlurEffect())
            {
                transformEffect.Source = bitmap;
                transformEffect.TransformMatrix = transform;
                transformEffect.InterpolationMode = CanvasImageInterpolation.Linear;

                blurEffect.Source = transformEffect;
                blurEffect.BlurAmount = blurAmount > 0 ? (blurAmount / 2.0f) : 0f;
                blurEffect.BorderMode = EffectBorderMode.Hard;

                ds.DrawImage(blurEffect, 0, 0, new Windows.Foundation.Rect(0, 0, screenSize.Width, screenSize.Height), alpha);
            }
        }

        public void Dispose()
        {
            _currentBitmap?.Dispose();
            _currentBitmap = null;
            _previousBitmap?.Dispose();
            _previousBitmap = null;
        }
    }
}