using System;
using System.Numerics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using SkiaSharp;

namespace BetterLyrics.Avalonia.Renderer;

public partial class CoverBackgroundRenderer : EffectRendererBase, IDisposable
{
    private readonly ValueTransition<double> _crossfadeTransition;

    private int _blurAmount = 100;
    
    // In Skia-native renderers, SKImage directly replaces CanvasBitmap
    private SKImage? _currentBitmap;
    private SKImage? _currentTargetCache;
    private SKImage? _previousBitmap;
    private SKImage? _previousTargetCache;

    private Size _lastScreenSize;
    private bool _lastWasRotating;

    private bool _needsCacheUpdate;
    private float _rotationAngle;

    private int _speed = 100;

    public CoverBackgroundRenderer()
    {
        _crossfadeTransition = new ValueTransition<double>(1.0,
            EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Linear), 0.7);
    }

    public bool IsEnabled { get; set; } = false;
    public int Opacity { get; set; } = 100;

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

    public void SetCoverBitmap(SKImage? newBitmap)
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

    public void Update(TimeSpan deltaTime, float bassEnergy, int breathingIntensity,
        bool is3DEnabled, Size controlSize)
    {
        if (!IsEnabled) return;

        UpdateBreathing(bassEnergy, breathingIntensity);

        _crossfadeTransition.Update(deltaTime);

        if (Speed > 0)
        {
            var baseSpeed = 0.6f;
            var currentSpeed = Speed / 100.0f * baseSpeed;
            _rotationAngle += currentSpeed * (float)deltaTime.TotalSeconds;
            _rotationAngle %= (float)(2 * Math.PI);
        }

        if (_crossfadeTransition.Value >= 1.0 && _previousBitmap != null)
        {
            _previousBitmap = null;
            _previousTargetCache?.Dispose();
            _previousTargetCache = null;
        }

        if (is3DEnabled)
        {
            var center = new Vector3((float)controlSize.Width / 2, (float)controlSize.Height / 2, 0);
            UpdateParallaxMatrix(center, true);
        }
        else
        {
            ResetParallaxMatrix();
        }
    }

    public void Draw(DrawingContext context, Rect bounds, bool isBreathingEffectEnabled)
    {
        if (!IsEnabled || Opacity <= 0) return;

        if (_lastScreenSize != bounds.Size)
        {
            _lastScreenSize = bounds.Size;
            _needsCacheUpdate = true;
        }

        var isRotating = Speed > 0;
        if (_lastWasRotating != isRotating)
        {
            _lastWasRotating = isRotating;
            _needsCacheUpdate = true;
        }

        EnsureCachedLayer(_currentBitmap, ref _currentTargetCache);

        context.Custom(new CoverCustomDrawOperation(this, bounds, isBreathingEffectEnabled));
    }

    private void RenderSkia(SKCanvas canvas, Rect bounds, bool isBreathingEffectEnabled)
    {
        var baseAlpha = Opacity / 100.0f;
        var angle = Speed > 0 ? _rotationAngle : 0f;
        var fadeProgress = _crossfadeTransition.Value;
        var isCrossfading = fadeProgress < 1.0 && _previousTargetCache != null;

        var screenCenter = new Vector2((float)bounds.Width / 2f, (float)bounds.Height / 2f);

        canvas.Save();

        // 1. Apply 3D Parallax Projection Matrix
        if (!_threeDimMatrix.IsIdentity)
        {
            // Note: ToSKMatrix is inherited from your updated EffectRendererBase
            canvas.Concat(ToSKMatrix(_threeDimMatrix));
        }

        // 2. Apply 2D Scale (Breathing)
        ApplyBreathingTransform(canvas, screenCenter, isBreathingEffectEnabled);

        // 3. Draw Layers
        Draw2DComposition(canvas, screenCenter, angle, baseAlpha, fadeProgress, isCrossfading);

        canvas.Restore();
    }

    private void EnsureCachedLayer(SKImage? sourceBitmap, ref SKImage? targetCache)
    {
        if (sourceBitmap == null)
        {
            targetCache?.Dispose();
            targetCache = null;
            return;
        }

        if (_needsCacheUpdate || targetCache == null)
        {
            targetCache?.Dispose();

            float imgW = sourceBitmap.Width;
            float imgH = sourceBitmap.Height;
            var screenSize = _lastScreenSize;

            float scale;
            if (_lastWasRotating) // Speed > 0
            {
                var screenDiagonal = (float)Math.Sqrt(screenSize.Width * screenSize.Width + screenSize.Height * screenSize.Height);
                scale = Math.Max(screenDiagonal / imgW, screenDiagonal / imgH);
            }
            else
            {
                var scaleX = (float)screenSize.Width / imgW;
                var scaleY = (float)screenSize.Height / imgH;
                scale = Math.Max(scaleX, scaleY);
            }

            var targetW = (int)Math.Ceiling(imgW * scale);
            var targetH = (int)Math.Ceiling(imgH * scale);

            // Replaces CanvasRenderTarget
            var info = new SKImageInfo(targetW, targetH);
            using var surface = SKSurface.Create(info);
            using var canvas = surface.Canvas;
            
            canvas.Clear(SKColors.Transparent);

            // Win2D Radius roughly translates to Sigma * 3
            float sigma = BlurAmount / 3f;

            using var paint = new SKPaint
            {
                IsAntialias = true,
                //FilterQuality = SKFilterQuality.High,
                ImageFilter = BlurAmount > 0 ? SKImageFilter.CreateBlur(sigma, sigma, SKShaderTileMode.Decal) : null
            };

            canvas.Save();
            canvas.Scale(scale);
            canvas.DrawImage(sourceBitmap, 0, 0, paint);
            canvas.Restore();

            // Snapshot the surface memory to a fixed image cache
            targetCache = surface.Snapshot();

            if (sourceBitmap == _currentBitmap) _needsCacheUpdate = false;
        }
    }

    private static void DrawCachedLayer(SKCanvas canvas, SKImage? cachedTexture,
        Vector2 screenCenter, float rotationRadians, float alpha)
    {
        if (cachedTexture == null) return;

        var textureCenter = new Vector2(cachedTexture.Width / 2f, cachedTexture.Height / 2f);

        using var paint = new SKPaint
        {
            Color = new SKColor(255, 255, 255, (byte)(alpha * 255)),
            IsAntialias = true,
            //FilterQuality = SKFilterQuality.High
        };

        canvas.Save();

        // Native Matrix Transformations: Translate -> Rotate -> Translate
        canvas.Translate(screenCenter.X, screenCenter.Y);
        canvas.RotateRadians(rotationRadians);
        canvas.Translate(-textureCenter.X, -textureCenter.Y);

        canvas.DrawImage(cachedTexture, 0, 0, paint);

        canvas.Restore();
    }

    private void Draw2DComposition(SKCanvas canvas, Vector2 screenCenter, float angle, float baseAlpha,
        double fadeProgress, bool isCrossfading)
    {
        if (isCrossfading)
        {
            DrawCachedLayer(canvas, _previousTargetCache, screenCenter, angle, baseAlpha);

            var newLayerAlpha = baseAlpha * (float)fadeProgress;
            DrawCachedLayer(canvas, _currentTargetCache, screenCenter, angle, newLayerAlpha);
        }
        else if (_currentTargetCache != null)
        {
            DrawCachedLayer(canvas, _currentTargetCache, screenCenter, angle, baseAlpha);
        }
    }

    // --- Skia Custom Draw Operation Adapter ---
    private sealed class CoverCustomDrawOperation : ICustomDrawOperation
    {
        private readonly CoverBackgroundRenderer _renderer;
        private readonly bool _isBreathingEnabled;

        public Rect Bounds { get; }

        public CoverCustomDrawOperation(CoverBackgroundRenderer renderer, Rect bounds, bool isBreathingEnabled)
        {
            _renderer = renderer;
            Bounds = bounds;
            _isBreathingEnabled = isBreathingEnabled;
        }

        public void Dispose() { }
        public bool HitTest(Point p) => false;
        public bool Equals(ICustomDrawOperation? other) => false;

        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature == null) return;

            using var lease = leaseFeature.Lease();
            _renderer.RenderSkia(lease.SkCanvas, Bounds, _isBreathingEnabled);
        }
    }
}