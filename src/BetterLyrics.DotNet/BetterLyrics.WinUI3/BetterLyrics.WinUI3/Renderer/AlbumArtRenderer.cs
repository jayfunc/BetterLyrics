using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Settings;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace BetterLyrics.WinUI3.Renderer;

/// <summary>
/// 播放页专辑封面的 Win2D 渲染：圆角、投影、切歌过渡、渐隐遮罩与 3D 视差。
/// 位图与静态图层按内容缓存，只在封面 / 尺寸 / 样式变化时重建。
/// </summary>
public sealed partial class AlbumArtRenderer : EffectRendererBase, IDisposable
{
    // ThemeShadow 没有直接的 Win2D 等价物，按阴影强度换算模糊半径与下移距离
    private const float ShadowBlurFactor = 0.8f;
    private const float ShadowOffsetFactor = 0.45f;
    private const byte ShadowAlpha = 96;

    private readonly object _gate = new();

    private readonly ValueTransition<double> _switchTransition = new(
        1.0,
        // 切歌过渡用减速缓动（快出慢停），线性会显得生硬
        EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Cubic, EaseMode.Out),
        Time.AnimationDuration.TotalSeconds);

    private CanvasBitmap? _currentBitmap;
    private CoverLayer? _currentLayer;

    private bool _hasPendingBitmap;
    private CanvasBitmap? _pendingBitmap;

    private CanvasBitmap? _previousBitmap;
    private CoverLayer? _previousLayer;

    /// <summary>
    /// 线程安全：UI 线程投递新封面，画布线程在下一帧 Update 中换入。
    /// </summary>
    public void SetCoverBitmap(CanvasBitmap? bitmap)
    {
        lock (_gate)
        {
            _pendingBitmap?.Dispose();
            _pendingBitmap = bitmap;
            _hasPendingBitmap = true;
        }
    }

    public void ResetDeviceResources()
    {
        DisposeLayer(ref _currentLayer);
        DisposeLayer(ref _previousLayer);

        _currentBitmap?.Dispose();
        _currentBitmap = null;
        _previousBitmap?.Dispose();
        _previousBitmap = null;

        lock (_gate)
        {
            _pendingBitmap?.Dispose();
            _pendingBitmap = null;
            _hasPendingBitmap = false;
        }

        _switchTransition.JumpTo(1.0);
    }

    public void Update(TimeSpan elapsedTime)
    {
        ApplyPendingBitmap();

        _switchTransition.Update(elapsedTime);

        // 过渡结束后释放上一张封面，避免长期占用两张位图与图层缓存
        if (_switchTransition.Value >= 1.0 && _previousBitmap != null)
        {
            DisposeLayer(ref _previousLayer);
            _previousBitmap.Dispose();
            _previousBitmap = null;
        }
    }

    /// <summary>
    /// 计算等比适配后封面图片的真实边界（屏幕坐标）。容器非方形时图片会在容器内
    /// 按布局对齐配置留白，环绕封面的频谱等叠加效果需要跟随这个边界。
    /// </summary>
    public Rect GetAlbumArtBounds(Rect containerBounds, AppHorizontalAlignment horizontal,
        AppVerticalAlignment vertical)
    {
        var bitmap = _currentBitmap ?? _previousBitmap;
        if (bitmap == null || containerBounds.Width < 1 || containerBounds.Height < 1)
            return containerBounds;

        var size = bitmap.Size;
        if (size.Width <= 0 || size.Height <= 0) return containerBounds;

        CalculateImageRect(containerBounds, size, horizontal, vertical, out var bounds);
        return bounds;
    }

    private static void CalculateImageRect(Rect container, Size bitmapSize,
        AppHorizontalAlignment horizontal, AppVerticalAlignment vertical, out Rect imageRect)
    {
        var scale = Math.Min(container.Width / bitmapSize.Width, container.Height / bitmapSize.Height);
        var width = bitmapSize.Width * scale;
        var height = bitmapSize.Height * scale;

        var x = horizontal switch
        {
            AppHorizontalAlignment.Left => container.X,
            AppHorizontalAlignment.Right => container.X + container.Width - width,
            _ => container.X + (container.Width - width) / 2
        };

        var y = vertical switch
        {
            AppVerticalAlignment.Top => container.Y,
            AppVerticalAlignment.Bottom => container.Y + container.Height - height,
            _ => container.Y + (container.Height - height) / 2
        };

        imageRect = new Rect(x, y, width, height);
    }

    public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds, Rect rect,
        LyricsWindowStatus status, AppHorizontalAlignment horizontal, AppVerticalAlignment vertical)
    {
        if (rect.Width < 1 || rect.Height < 1) return;

        var style = status.AlbumArtLayoutSettings;
        var effect = status.AlbumArtAreaEffectSettings;

        EnsureLayer(control, ref _currentLayer, _currentBitmap, rect, style, horizontal, vertical);
        EnsureLayer(control, ref _previousLayer, _previousBitmap, rect, style, horizontal, vertical);

        if (_currentLayer == null && _previousLayer == null) return;

        // 渐隐遮罩、滑入距离与视差旋转都以真实图片边界为基准，
        // 否则左/右或上/下对齐时会出现位置偏移
        var artBounds = GetAlbumArtBounds(rect, horizontal, vertical);

        var center = new Vector3((float)(artBounds.X + artBounds.Width / 2),
            (float)(artBounds.Y + artBounds.Height / 2), 0);
        if (effect.IsParallaxEnabled)
            UpdateParallaxMatrix(center, true);
        else
            ResetParallaxMatrix();

        if (_threeDimMatrix.IsIdentity)
        {
            DrawScene(control, ds, artBounds, effect);
            return;
        }

        using var scene = new CanvasCommandList(control);
        using (var sceneDs = scene.CreateDrawingSession())
        {
            DrawScene(control, sceneDs, artBounds, effect);
        }

        DrawWithParallax(ds, scene);
    }

    public void Dispose()
    {
        DisposeLayer(ref _currentLayer);
        DisposeLayer(ref _previousLayer);

        _currentBitmap?.Dispose();
        _currentBitmap = null;
        _previousBitmap?.Dispose();
        _previousBitmap = null;

        lock (_gate)
        {
            _pendingBitmap?.Dispose();
            _pendingBitmap = null;
            _hasPendingBitmap = false;
        }
    }

    private void ApplyPendingBitmap()
    {
        CanvasBitmap? next;

        lock (_gate)
        {
            if (!_hasPendingBitmap) return;

            _hasPendingBitmap = false;
            next = _pendingBitmap;
            _pendingBitmap = null;
        }

        DisposeLayer(ref _previousLayer);
        _previousBitmap?.Dispose();

        _previousBitmap = _currentBitmap;
        _previousLayer = _currentLayer;
        _currentLayer = null;
        _currentBitmap = next;

        if (_previousBitmap == null)
        {
            _switchTransition.JumpTo(1.0);
            return;
        }

        _switchTransition.JumpTo(0.0);
        _switchTransition.Start(1.0);
    }

    private void DrawScene(ICanvasResourceCreator control, CanvasDrawingSession ds, Rect rect,
        AlbumArtAreaEffectSettings effect)
    {
        if (!effect.FadeOut)
        {
            DrawLayers(ds, rect, effect.ImageSwitchType);
            return;
        }

        using var brush = CreateFadeBrush(control, rect, effect);
        using (ds.CreateLayer(brush))
        {
            DrawLayers(ds, rect, effect.ImageSwitchType);
        }
    }

    private void DrawLayers(CanvasDrawingSession ds, Rect rect, ImageSwitchType switchType)
    {
        var progress = _switchTransition.Value;
        var isCrossfade = switchType == ImageSwitchType.Crossfade;
        var width = (float)rect.Width;

        var previousAlpha = (float)(1.0 - progress);
        var currentAlpha = (float)progress;
        var previousOffset = isCrossfade ? 0f : -width * (float)progress;
        var currentOffset = isCrossfade ? 0f : -width * previousAlpha;

        if (_previousLayer != null && previousAlpha > 0.001f)
            DrawLayer(ds, _previousLayer, previousOffset, previousAlpha);

        if (_currentLayer != null && currentAlpha > 0.001f)
            DrawLayer(ds, _currentLayer, currentOffset, currentAlpha);
    }

    private static void DrawLayer(CanvasDrawingSession ds, CoverLayer layer, float offsetX, float alpha)
    {
        if (layer.Shadow != null)
            ds.DrawImage(layer.Shadow, layer.Origin.X + offsetX, layer.Origin.Y + layer.ShadowOffsetY,
                layer.SourceRect, alpha);

        ds.DrawImage(layer.Cover, layer.Origin.X + offsetX, layer.Origin.Y, layer.SourceRect, alpha);
    }

    private static CanvasLinearGradientBrush CreateFadeBrush(ICanvasResourceCreator control, Rect rect,
        AlbumArtAreaEffectSettings effect)
    {
        // 渐变方向按封面区域归一化，与原先 OpacityMaskView 在 -32 边距内的换算结果一致
        var start = new Vector2(
            (float)(rect.X + rect.Width * effect.FadeOutStartPointX),
            (float)(rect.Y + rect.Height * effect.FadeOutStartPointY));
        var end = new Vector2(
            (float)(rect.X + rect.Width * effect.FadeOutEndPointX),
            (float)(rect.Y + rect.Height * effect.FadeOutEndPointY));

        return new CanvasLinearGradientBrush(control, new[]
        {
            new CanvasGradientStop { Position = 0f, Color = Microsoft.UI.Colors.White },
            new CanvasGradientStop { Position = 1f, Color = Microsoft.UI.Colors.Transparent }
        })
        {
            StartPoint = start,
            EndPoint = end
        };
    }

    private static void EnsureLayer(ICanvasAnimatedControl control, ref CoverLayer? layer, CanvasBitmap? bitmap,
        Rect rect, AlbumArtAreaStyleSettings style, AppHorizontalAlignment horizontal,
        AppVerticalAlignment vertical)
    {
        if (bitmap == null)
        {
            DisposeLayer(ref layer);
            return;
        }

        if (layer != null && layer.Matches(control, bitmap, rect, style, horizontal, vertical)) return;

        DisposeLayer(ref layer);
        layer = BuildLayer(control, bitmap, rect, style, horizontal, vertical);
    }

    private static CoverLayer BuildLayer(ICanvasAnimatedControl control, CanvasBitmap bitmap, Rect rect,
        AlbumArtAreaStyleSettings style, AppHorizontalAlignment horizontal, AppVerticalAlignment vertical)
    {
        var width = (float)rect.Width;
        var height = (float)rect.Height;

        var bitmapSize = bitmap.Size;
        CalculateImageRect(new Rect(0, 0, width, height), bitmapSize, horizontal, vertical, out var imageLayout);

        var imageWidth = (float)imageLayout.Width;
        var imageHeight = (float)imageLayout.Height;
        var imageScale = (float)(bitmapSize.Width > 0 ? imageWidth / bitmapSize.Width : 1);

        var radius = (float)(style.CoverImageRadius / 100.0 * Math.Min(imageWidth, imageHeight) / 2.0);

        var shadowAmount = Math.Max(0, style.CoverImageShadowAmount);
        var shadowBlur = shadowAmount * ShadowBlurFactor;
        var shadowOffsetY = shadowAmount * ShadowOffsetFactor;
        var padding = shadowAmount > 0
            ? (float)Math.Ceiling(shadowBlur * 1.5 + Math.Abs(shadowOffsetY)) + 1f
            : 0f;

        var targetWidth = width + padding * 2;
        var targetHeight = height + padding * 2;
        var dpi = control.Dpi;

        var cover = new CanvasRenderTarget(control, targetWidth, targetHeight, dpi);
        using (var ds = cover.CreateDrawingSession())
        {
            ds.Clear(Microsoft.UI.Colors.Transparent);

            var imageRect = new Rect(
                padding + imageLayout.X,
                padding + imageLayout.Y,
                imageWidth,
                imageHeight);

            // CanvasImageBrush 按图片原始尺寸平铺，不会自动拉伸到填充区域，
            // 这里用 Transform 把图片缩放到目标尺寸并对齐到 imageRect
            using var brush = new CanvasImageBrush(control, bitmap)
            {
                Interpolation = CanvasImageInterpolation.HighQualityCubic,
                Transform = Matrix3x2.CreateScale(imageScale) *
                            Matrix3x2.CreateTranslation((float)imageRect.X, (float)imageRect.Y)
            };

            ds.FillRoundedRectangle(imageRect, radius, radius, brush);
        }

        CanvasRenderTarget? shadow = null;
        if (shadowAmount > 0)
        {
            shadow = new CanvasRenderTarget(control, targetWidth, targetHeight, dpi);
            using var ds = shadow.CreateDrawingSession();
            ds.Clear(Microsoft.UI.Colors.Transparent);

            using var silhouette = new ColorSourceEffect
            {
                Color = Color.FromArgb(ShadowAlpha, 0, 0, 0)
            };
            using var alphaMask = new AlphaMaskEffect
            {
                Source = silhouette,
                AlphaMask = cover
            };
            using var blur = new GaussianBlurEffect
            {
                Source = alphaMask,
                BlurAmount = shadowBlur,
                BorderMode = EffectBorderMode.Soft,
                Optimization = EffectOptimization.Quality
            };

            ds.DrawImage(blur);
        }

        return new CoverLayer(
            cover,
            shadow,
            new Vector2((float)rect.X - padding, (float)rect.Y - padding),
            new Rect(0, 0, targetWidth, targetHeight),
            shadowOffsetY,
            bitmap,
            rect,
            style.CoverImageRadius,
            shadowAmount,
            dpi,
            control.Device,
            horizontal,
            vertical);
    }

    private static void DisposeLayer(ref CoverLayer? layer)
    {
        layer?.Dispose();
        layer = null;
    }

    private sealed class CoverLayer : IDisposable
    {
        private readonly float _dpi;
        private readonly CanvasDevice _device;
        private readonly Rect _rect;
        private readonly int _radius;
        private readonly int _shadowAmount;
        private readonly AppHorizontalAlignment _horizontal;
        private readonly AppVerticalAlignment _vertical;

        public CoverLayer(CanvasRenderTarget cover, CanvasRenderTarget? shadow, Vector2 origin,
            Rect sourceRect, float shadowOffsetY, CanvasBitmap bitmap, Rect rect, int radius,
            int shadowAmount, float dpi, CanvasDevice device, AppHorizontalAlignment horizontal,
            AppVerticalAlignment vertical)
        {
            Cover = cover;
            Shadow = shadow;
            Origin = origin;
            SourceRect = sourceRect;
            ShadowOffsetY = shadowOffsetY;
            Bitmap = bitmap;
            _rect = rect;
            _radius = radius;
            _shadowAmount = shadowAmount;
            _dpi = dpi;
            _device = device;
            _horizontal = horizontal;
            _vertical = vertical;
        }

        public CanvasRenderTarget Cover { get; }
        public CanvasRenderTarget? Shadow { get; }
        public Vector2 Origin { get; }
        public Rect SourceRect { get; }
        public float ShadowOffsetY { get; }
        public CanvasBitmap Bitmap { get; }

        public bool Matches(ICanvasAnimatedControl control, CanvasBitmap bitmap, Rect rect,
            AlbumArtAreaStyleSettings style, AppHorizontalAlignment horizontal, AppVerticalAlignment vertical) =>
            ReferenceEquals(Bitmap, bitmap) &&
            ReferenceEquals(_device, control.Device) &&
            Math.Abs(_dpi - control.Dpi) < 0.01f &&
            _rect.Equals(rect) &&
            _radius == style.CoverImageRadius &&
            _shadowAmount == Math.Max(0, style.CoverImageShadowAmount) &&
            _horizontal == horizontal &&
            _vertical == vertical;

        public void Dispose()
        {
            Cover.Dispose();
            Shadow?.Dispose();
        }
    }
}