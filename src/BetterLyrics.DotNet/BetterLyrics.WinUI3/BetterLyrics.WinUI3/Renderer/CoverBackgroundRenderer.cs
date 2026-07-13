using System;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace BetterLyrics.WinUI3.Renderer;

public partial class CoverBackgroundRenderer : EffectRendererBase, IDisposable
{
    private readonly ValueTransition<double> _crossfadeTransition;

    private int _blurAmount = 100;
    private CanvasBitmap? _currentBitmap;

    private CanvasRenderTarget? _currentTargetCache;

    private Size _lastScreenSize;
    private bool _lastWasRotating;

    private bool _needsCacheUpdate;
    private CanvasBitmap? _previousBitmap;
    private CanvasRenderTarget? _previousTargetCache;
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

    public void Update(ICanvasAnimatedControl control, TimeSpan deltaTime, float bassEnergy, int breathingIntensity,
        bool is3DEnabled)
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
            var center = new Vector3((float)control.Size.Width / 2, (float)control.Size.Height / 2, 0);
            UpdateParallaxMatrix(center, true);
        }
        else
        {
            ResetParallaxMatrix();
        }
    }

    public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds, bool isBreathingEffectEnabled)
    {
        if (!IsEnabled || Opacity <= 0) return;

        if (_lastScreenSize != control.Size)
        {
            _lastScreenSize = control.Size;
            _needsCacheUpdate = true;
        }

        var isRotating = Speed > 0;
        if (_lastWasRotating != isRotating)
        {
            _lastWasRotating = isRotating;
            _needsCacheUpdate = true;
        }

        EnsureCachedLayer(control, _currentBitmap, ref _currentTargetCache);

        var baseAlpha = Opacity / 100.0f;
        var angle = isRotating ? _rotationAngle : 0f;
        var fadeProgress = _crossfadeTransition.Value;
        var isCrossfading = fadeProgress < 1.0 && _previousTargetCache != null;

        var screenCenter = new Vector2((float)control.Size.Width / 2f, (float)control.Size.Height / 2f);

        ApplyBreathingTransform(ds, screenCenter, isBreathingEffectEnabled);

        if (!_threeDimMatrix.IsIdentity)
        {
            using var commandList = new CanvasCommandList(control);
            using (var layerDs = commandList.CreateDrawingSession())
            {
                Draw2DComposition(layerDs, screenCenter, angle, baseAlpha, fadeProgress, isCrossfading);
            }

            DrawWithParallax(ds, commandList);
        }
        else
        {
            Draw2DComposition(ds, screenCenter, angle, baseAlpha, fadeProgress, isCrossfading);
        }

        ResetTransform(ds, isBreathingEffectEnabled);
    }

    private void EnsureCachedLayer(ICanvasResourceCreator resourceCreator, CanvasBitmap? sourceBitmap,
        ref CanvasRenderTarget? targetCache)
    {
        if (sourceBitmap == null)
        {
            targetCache?.Dispose();
            targetCache = null;
            return;
        }

        var deviceMismatch = targetCache != null && targetCache.Device != resourceCreator.Device;

        if (_needsCacheUpdate || targetCache == null || deviceMismatch)
        {
            targetCache?.Dispose();

            float imgW = sourceBitmap.SizeInPixels.Width;
            float imgH = sourceBitmap.SizeInPixels.Height;
            var screenSize = _lastScreenSize;

            float scale;
            if (_lastWasRotating) // Speed > 0
            {
                var screenDiagonal =
                    (float)Math.Sqrt(screenSize.Width * screenSize.Width + screenSize.Height * screenSize.Height);
                scale = Math.Max(screenDiagonal / imgW, screenDiagonal / imgH);
            }
            else
            {
                var scaleX = (float)screenSize.Width / imgW;
                var scaleY = (float)screenSize.Height / imgH;
                scale = Math.Max(scaleX, scaleY);
            }

            var targetW = imgW * scale;
            var targetH = imgH * scale;

            targetCache = new CanvasRenderTarget(resourceCreator, targetW, targetH, sourceBitmap.Dpi);

            using (var ds = targetCache.CreateDrawingSession())
            {
                ds.Clear(Color.FromArgb(0, 0, 0, 0));

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

            if (sourceBitmap == _currentBitmap) _needsCacheUpdate = false;
        }
    }

    private static void DrawCachedLayer(CanvasDrawingSession ds, CanvasRenderTarget? cachedTexture,
        Vector2 screenCenter, float rotationRadians, float alpha)
    {
        if (cachedTexture == null) return;

        var textureCenter = new Vector2((float)cachedTexture.Size.Width / 2f, (float)cachedTexture.Size.Height / 2f);

        var transform =
            Matrix3x2.CreateTranslation(-textureCenter) * Matrix3x2.CreateRotation(rotationRadians) *
            Matrix3x2.CreateTranslation(screenCenter);

        var previousTransform = ds.Transform;

        ds.Transform = transform * previousTransform;
        ds.DrawImage(cachedTexture, 0, 0, new Rect(0, 0, cachedTexture.Size.Width, cachedTexture.Size.Height), alpha);

        ds.Transform = previousTransform;
    }

    private void Draw2DComposition(CanvasDrawingSession ds, Vector2 screenCenter, float angle, float baseAlpha,
        double fadeProgress, bool isCrossfading)
    {
        if (isCrossfading)
        {
            DrawCachedLayer(ds, _previousTargetCache, screenCenter, angle, baseAlpha);

            var newLayerAlpha = baseAlpha * (float)fadeProgress;
            DrawCachedLayer(ds, _currentTargetCache, screenCenter, angle, newLayerAlpha);
        }
        else if (_currentTargetCache != null)
        {
            DrawCachedLayer(ds, _currentTargetCache, screenCenter, angle, baseAlpha);
        }
    }
}