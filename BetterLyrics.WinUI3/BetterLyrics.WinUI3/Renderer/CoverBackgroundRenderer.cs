using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Numerics;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Renderer
{
    public partial class CoverBackgroundRenderer : IDisposable
    {
        private CanvasBitmap? _currentBitmap;
        private CanvasBitmap? _previousBitmap;

        private CanvasRenderTarget? _currentTargetCache;
        private CanvasRenderTarget? _previousTargetCache;

        private Size _lastScreenSize;
        private bool _lastWasRotating = false;

        private readonly ValueTransition<double> _crossfadeTransition;
        private float _rotationAngle = 0f;

        public bool IsEnabled { get; set; } = false;
        public int Opacity { get; set; } = 100;

        private bool _needsCacheUpdate = false;

        private int _blurAmount = 100;
        public int BlurAmount
        {
            get => _blurAmount;
            set
            {
                if (_blurAmount != value)
                {
                    _blurAmount = value;
                    _needsCacheUpdate = true;
                }
            }
        }

        private int _speed = 100;
        public int Speed
        {
            get => _speed;
            set
            {
                if (_speed != value)
                {
                    _speed = value;
                    _needsCacheUpdate = true;
                }
            }
        }

        public CoverBackgroundRenderer()
        {
            _crossfadeTransition = new ValueTransition<double>(1.0, EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Linear), 0.7);
        }

        public void SetCoverBitmap(CanvasBitmap? newBitmap)
        {
            if (_currentBitmap == newBitmap) return;

            _previousBitmap = _currentBitmap;
            _previousTargetCache = _currentTargetCache;
            _currentTargetCache = null;

            _currentBitmap = newBitmap;

            if (_currentBitmap == null)
            {
                _crossfadeTransition.JumpTo(1.0);
            }
            else
            {
                if (_previousBitmap == null)
                {
                    _crossfadeTransition.JumpTo(1.0);
                }
                else
                {
                    _crossfadeTransition.JumpTo(0.0);
                    _crossfadeTransition.Start(1.0);
                }
            }

            _needsCacheUpdate = true;
        }

        public void Update(TimeSpan deltaTime)
        {
            if (!IsEnabled) return;

            _crossfadeTransition.Update(deltaTime);

            if (Speed > 0)
            {
                float baseSpeed = 0.6f;
                float currentSpeed = (Speed / 100.0f) * baseSpeed;
                _rotationAngle += currentSpeed * (float)deltaTime.TotalSeconds;
                _rotationAngle %= (float)(2 * Math.PI);
            }

            if (_crossfadeTransition.Value >= 1.0 && _previousBitmap != null)
            {
                _previousBitmap = null;
                _previousTargetCache?.Dispose();
                _previousTargetCache = null;
            }
        }

        public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (!IsEnabled || Opacity <= 0) return;

            if (_lastScreenSize != control.Size)
            {
                _lastScreenSize = control.Size;
                _needsCacheUpdate = true;
            }

            bool isRotating = Speed > 0;
            if (_lastWasRotating != isRotating)
            {
                _lastWasRotating = isRotating;
                _needsCacheUpdate = true;
            }

            EnsureCachedLayer(control, _currentBitmap, ref _currentTargetCache);

            float baseAlpha = Opacity / 100.0f;
            float angle = isRotating ? _rotationAngle : 0f;
            double fadeProgress = _crossfadeTransition.Value;
            bool isCrossfading = fadeProgress < 1.0 && _previousTargetCache != null;

            Vector2 screenCenter = new Vector2((float)control.Size.Width / 2f, (float)control.Size.Height / 2f);

            if (isCrossfading)
            {
                DrawCachedLayer(ds, _previousTargetCache, screenCenter, angle, baseAlpha);

                float newLayerAlpha = baseAlpha * (float)fadeProgress;
                DrawCachedLayer(ds, _currentTargetCache, screenCenter, angle, newLayerAlpha);
            }
            else if (_currentTargetCache != null)
            {
                DrawCachedLayer(ds, _currentTargetCache, screenCenter, angle, baseAlpha);
            }
        }

        private void EnsureCachedLayer(ICanvasResourceCreator resourceCreator, CanvasBitmap? sourceBitmap, ref CanvasRenderTarget? targetCache)
        {
            if (sourceBitmap == null)
            {
                targetCache?.Dispose();
                targetCache = null;
                return;
            }

            bool deviceMismatch = targetCache != null && targetCache.Device != resourceCreator.Device;

            if (_needsCacheUpdate || targetCache == null || deviceMismatch)
            {
                targetCache?.Dispose();

                float imgW = sourceBitmap.SizeInPixels.Width;
                float imgH = sourceBitmap.SizeInPixels.Height;
                Size screenSize = _lastScreenSize;

                float scale;
                if (_lastWasRotating) // Speed > 0
                {
                    float screenDiagonal = (float)Math.Sqrt(screenSize.Width * screenSize.Width + screenSize.Height * screenSize.Height);
                    scale = Math.Max(screenDiagonal / imgW, screenDiagonal / imgH);
                }
                else
                {
                    float scaleX = (float)screenSize.Width / imgW;
                    float scaleY = (float)screenSize.Height / imgH;
                    scale = Math.Max(scaleX, scaleY);
                }

                float targetW = imgW * scale;
                float targetH = imgH * scale;

                targetCache = new CanvasRenderTarget(resourceCreator, targetW, targetH, sourceBitmap.Dpi);

                using (var ds = targetCache.CreateDrawingSession())
                {
                    ds.Clear(Windows.UI.Color.FromArgb(0, 0, 0, 0));

                    using (var transformEffect = new Transform2DEffect())
                    using (var blurEffect = new GaussianBlurEffect())
                    {
                        transformEffect.Source = sourceBitmap;
                        transformEffect.TransformMatrix = Matrix3x2.CreateScale(scale);
                        transformEffect.InterpolationMode = CanvasImageInterpolation.Linear;

                        blurEffect.Source = transformEffect;
                        blurEffect.BlurAmount = BlurAmount;
                        blurEffect.BorderMode = EffectBorderMode.Hard;

                        ds.DrawImage(blurEffect);
                    }
                }

                if (sourceBitmap == _currentBitmap)
                {
                    _needsCacheUpdate = false;
                }
            }
        }

        private void DrawCachedLayer(CanvasDrawingSession ds, CanvasRenderTarget? cachedTexture, Vector2 screenCenter, float rotationRadians, float alpha)
        {
            if (cachedTexture == null) return;

            Vector2 textureCenter = new Vector2((float)cachedTexture.Size.Width / 2f, (float)cachedTexture.Size.Height / 2f);

            Matrix3x2 transform =
                Matrix3x2.CreateTranslation(-textureCenter) * Matrix3x2.CreateRotation(rotationRadians) * Matrix3x2.CreateTranslation(screenCenter);

            Matrix3x2 previousTransform = ds.Transform;

            ds.Transform = transform * previousTransform;
            ds.DrawImage(cachedTexture, 0, 0, new Rect(0, 0, cachedTexture.Size.Width, cachedTexture.Size.Height), alpha);

            ds.Transform = previousTransform;
        }

        public void Dispose()
        {
            _currentBitmap?.Dispose();
            _previousBitmap?.Dispose();

            _currentTargetCache?.Dispose();
            _previousTargetCache?.Dispose();

            _currentBitmap = null;
            _previousBitmap = null;
            _currentTargetCache = null;
            _previousTargetCache = null;
        }
    }
}