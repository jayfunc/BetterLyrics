using System;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.UI;

namespace BetterLyrics.WinUI3.Renderer;

public partial class EdgeFadeMaskRenderer : IDisposable
{
    // 状态缓存
    private MaskRenderMode _currentMode = MaskRenderMode.None;
    private float _lastBottom;
    private Rect _lastBounds = new(0, 0, 0, 0);
    private bool _lastIsVertical;

    // EdgeFade 模式专有缓存
    private float _lastLeft;
    private float _lastRight;

    // GradientStops 模式专有缓存
    private CanvasGradientStop[]? _lastStops;
    private float _lastTop;

    private CanvasCommandList? _maskCommandList;

    public CanvasImageBrush? Brush { get; private set; }

    public void Dispose()
    {
        Brush?.Dispose();
        _maskCommandList?.Dispose();
    }

    public void Update(ICanvasResourceCreator resourceCreator, float width, float height,
        float fadeLeftPercentage, float fadeTopPercentage, float fadeRightPercentage, float fadeBottomPercentage)
    {
        var fadeLeft = fadeLeftPercentage / 100f * (width / 2f);
        var fadeTop = fadeTopPercentage / 100f * (height / 2f);
        var fadeRight = fadeRightPercentage / 100f * (width / 2f);
        var fadeBottom = fadeBottomPercentage / 100f * (height / 2f);

        var bounds = new Rect(0, 0, width, height);
        UpdateCore(resourceCreator, bounds, fadeLeft, fadeTop, fadeRight, fadeBottom);
    }

    public void Update(ICanvasResourceCreator resourceCreator, Rect targetRect, float fadeWidth)
    {
        UpdateCore(resourceCreator, targetRect, fadeWidth, fadeWidth, fadeWidth, fadeWidth);
    }

    public void Update(ICanvasResourceCreator resourceCreator, Rect targetRect,
        float fadeLeft, float fadeTop, float fadeRight, float fadeBottom)
    {
        UpdateCore(resourceCreator, targetRect, fadeLeft, fadeTop, fadeRight, fadeBottom);
    }

    private void UpdateCore(ICanvasResourceCreator resourceCreator, Rect bounds,
        float fadeLeft, float fadeTop, float fadeRight, float fadeBottom)
    {
        // 缓存判断
        if (_currentMode == MaskRenderMode.EdgeFade &&
            Math.Abs(_lastBounds.X - bounds.X) < 0.1f && Math.Abs(_lastBounds.Y - bounds.Y) < 0.1f &&
            Math.Abs(_lastBounds.Width - bounds.Width) < 0.1f && Math.Abs(_lastBounds.Height - bounds.Height) < 0.1f &&
            Math.Abs(_lastTop - fadeTop) < 0.1f && Math.Abs(_lastBottom - fadeBottom) < 0.1f &&
            Math.Abs(_lastLeft - fadeLeft) < 0.1f && Math.Abs(_lastRight - fadeRight) < 0.1f &&
            Brush != null)
            return;

        Brush?.Dispose();
        _maskCommandList?.Dispose();

        _maskCommandList = new CanvasCommandList(resourceCreator);

        var width = (float)bounds.Width;
        var height = (float)bounds.Height;
        var startX = (float)bounds.X;
        var startY = (float)bounds.Y;

        using (var ds = _maskCommandList.CreateDrawingSession())
        {
            ds.Clear(Color.FromArgb(0, 0, 0, 0));

            fadeLeft = Math.Clamp(fadeLeft, 0, width / 2f);
            fadeRight = Math.Clamp(fadeRight, 0, width / 2f);
            fadeTop = Math.Clamp(fadeTop, 0, height / 2f);
            fadeBottom = Math.Clamp(fadeBottom, 0, height / 2f);

            var centerW = width - fadeLeft - fadeRight;
            var centerH = height - fadeTop - fadeBottom;

            if (centerW > 0 && centerH > 0) ds.FillRectangle(fadeLeft, fadeTop, centerW, centerH, Colors.White);

            if (fadeTop > 0 && centerW > 0)
            {
                using var topBrush = new CanvasLinearGradientBrush(resourceCreator, Colors.Transparent, Colors.White)
                    { StartPoint = new Vector2(0, 0), EndPoint = new Vector2(0, fadeTop) };
                ds.FillRectangle(fadeLeft, 0, centerW, fadeTop, topBrush);
            }

            if (fadeBottom > 0 && centerW > 0)
            {
                using var bottomBrush = new CanvasLinearGradientBrush(resourceCreator, Colors.White, Colors.Transparent)
                    { StartPoint = new Vector2(0, height - fadeBottom), EndPoint = new Vector2(0, height) };
                ds.FillRectangle(fadeLeft, height - fadeBottom, centerW, fadeBottom, bottomBrush);
            }

            if (fadeLeft > 0 && centerH > 0)
            {
                using var leftBrush = new CanvasLinearGradientBrush(resourceCreator, Colors.Transparent, Colors.White)
                    { StartPoint = new Vector2(0, 0), EndPoint = new Vector2(fadeLeft, 0) };
                ds.FillRectangle(0, fadeTop, fadeLeft, centerH, leftBrush);
            }

            if (fadeRight > 0 && centerH > 0)
            {
                using var rightBrush = new CanvasLinearGradientBrush(resourceCreator, Colors.White, Colors.Transparent)
                    { StartPoint = new Vector2(width - fadeRight, 0), EndPoint = new Vector2(width, 0) };
                ds.FillRectangle(width - fadeRight, fadeTop, fadeRight, centerH, rightBrush);
            }

            DrawCorner(resourceCreator, ds, 0, 0, fadeLeft, fadeTop, new Vector2(fadeLeft, fadeTop));
            DrawCorner(resourceCreator, ds, width - fadeRight, 0, fadeRight, fadeTop,
                new Vector2(width - fadeRight, fadeTop));
            DrawCorner(resourceCreator, ds, 0, height - fadeBottom, fadeLeft, fadeBottom,
                new Vector2(fadeLeft, height - fadeBottom));
            DrawCorner(resourceCreator, ds, width - fadeRight, height - fadeBottom, fadeRight, fadeBottom,
                new Vector2(width - fadeRight, height - fadeBottom));
        }

        Brush = new CanvasImageBrush(resourceCreator, _maskCommandList)
        {
            SourceRectangle = new Rect(0, 0, width, height),
            Transform = Matrix3x2.CreateTranslation(startX, startY)
        };

        // 更新缓存
        _currentMode = MaskRenderMode.EdgeFade;
        _lastBounds = bounds;
        _lastTop = fadeTop;
        _lastBottom = fadeBottom;
        _lastLeft = fadeLeft;
        _lastRight = fadeRight;
    }

    private void DrawCorner(ICanvasResourceCreator resourceCreator, CanvasDrawingSession ds,
        float x, float y, float w, float h, Vector2 center)
    {
        if (w <= 0 || h <= 0) return;
        using var radialBrush = new CanvasRadialGradientBrush(resourceCreator, Colors.White, Colors.Transparent)
            { Center = center, RadiusX = w, RadiusY = h };
        ds.FillRectangle(x, y, w, h, radialBrush);
    }

    /// <summary>
    ///     使用 CanvasGradientStop 数组自定义多个完全可见的区域和渐变
    /// </summary>
    /// <param name="stops">渐变节点（Position从 0.0 到 1.0，必须递增）</param>
    /// <param name="isVertical">True表示垂直渐变（Y 轴），False表示水平渐变（X 轴）</param>
    public void Update(ICanvasResourceCreator resourceCreator, Rect bounds, CanvasGradientStop[] stops,
        bool isVertical = true)
    {
        // 缓存拦截检查
        if (_currentMode == MaskRenderMode.GradientStops &&
            Math.Abs(_lastBounds.X - bounds.X) < 0.1f && Math.Abs(_lastBounds.Y - bounds.Y) < 0.1f &&
            Math.Abs(_lastBounds.Width - bounds.Width) < 0.1f && Math.Abs(_lastBounds.Height - bounds.Height) < 0.1f &&
            _lastIsVertical == isVertical &&
            AreStopsEqual(_lastStops, stops) &&
            Brush != null)
            return;

        Brush?.Dispose();
        _maskCommandList?.Dispose();
        _maskCommandList = new CanvasCommandList(resourceCreator);

        var width = (float)bounds.Width;
        var height = (float)bounds.Height;
        var startX = (float)bounds.X;
        var startY = (float)bounds.Y;

        using (var ds = _maskCommandList.CreateDrawingSession())
        {
            // 清空背景为透明
            ds.Clear(Color.FromArgb(0, 0, 0, 0));

            var startPoint = new Vector2(0, 0);
            // 根据方向决定渐变的终点（垂直就是 Y 到底，水平就是 X 到底）
            var endPoint = isVertical ? new Vector2(0, height) : new Vector2(width, 0);

            // 使用传入的 Stops 建立线性渐变笔刷
            using var multiStopBrush = new CanvasLinearGradientBrush(resourceCreator, stops)
            {
                StartPoint = startPoint,
                EndPoint = endPoint
            };

            ds.FillRectangle(0, 0, width, height, multiStopBrush);
        }

        Brush = new CanvasImageBrush(resourceCreator, _maskCommandList)
        {
            SourceRectangle = new Rect(0, 0, width, height),
            Transform = Matrix3x2.CreateTranslation(startX, startY)
        };

        // 更新缓存
        _currentMode = MaskRenderMode.GradientStops;
        _lastBounds = bounds;
        _lastIsVertical = isVertical;
        _lastStops = stops.ToArray(); // 拷贝数组避免外部修改导致缓存失效判断错乱
    }

    private bool AreStopsEqual(CanvasGradientStop[]? a, CanvasGradientStop[]? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++)
            if (Math.Abs(a[i].Position - b[i].Position) > 0.001f || a[i].Color != b[i].Color)
                return false;
        return true;
    }

    private enum MaskRenderMode
    {
        None,
        EdgeFade,
        GradientStops
    }
}