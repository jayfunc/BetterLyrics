using System;
using System.Linq;
using Avalonia;
using Avalonia.Media;

namespace BetterLyrics.Avalonia.Renderer;

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
    private GradientStop[]? _lastStops;
    private float _lastTop;

    // 💡 最终暴露给外面 DrawingContext.PushOpacityMask 使用的画笔
    public IBrush? Brush { get; private set; }

    public void Dispose()
    {
        Brush = null;
    }

    public void Update(float width, float height,
        float fadeLeftPercentage, float fadeTopPercentage, float fadeRightPercentage, float fadeBottomPercentage)
    {
        var fadeLeft = fadeLeftPercentage / 100f * (width / 2f);
        var fadeTop = fadeTopPercentage / 100f * (height / 2f);
        var fadeRight = fadeRightPercentage / 100f * (width / 2f);
        var fadeBottom = fadeBottomPercentage / 100f * (height / 2f);

        var bounds = new Rect(0, 0, width, height);
        UpdateCore(bounds, fadeLeft, fadeTop, fadeRight, fadeBottom);
    }

    public void Update(Rect targetRect, float fadeWidth)
    {
        UpdateCore(targetRect, fadeWidth, fadeWidth, fadeWidth, fadeWidth);
    }

    public void Update(Rect targetRect, float fadeLeft, float fadeTop, float fadeRight, float fadeBottom)
    {
        UpdateCore(targetRect, fadeLeft, fadeTop, fadeRight, fadeBottom);
    }

    private void UpdateCore(Rect bounds, float fadeLeft, float fadeTop, float fadeRight, float fadeBottom)
    {
        // 缓存判断
        if (_currentMode == MaskRenderMode.EdgeFade &&
            Math.Abs(_lastBounds.X - bounds.X) < 0.1f && Math.Abs(_lastBounds.Y - bounds.Y) < 0.1f &&
            Math.Abs(_lastBounds.Width - bounds.Width) < 0.1f && Math.Abs(_lastBounds.Height - bounds.Height) < 0.1f &&
            Math.Abs(_lastTop - fadeTop) < 0.1f && Math.Abs(_lastBottom - fadeBottom) < 0.1f &&
            Math.Abs(_lastLeft - fadeLeft) < 0.1f && Math.Abs(_lastRight - fadeRight) < 0.1f &&
            Brush != null)
            return;

        var width = (float)bounds.Width;
        var height = (float)bounds.Height;
        var startX = (float)bounds.X;
        var startY = (float)bounds.Y;

        fadeLeft = Math.Clamp(fadeLeft, 0, width / 2f);
        fadeRight = Math.Clamp(fadeRight, 0, width / 2f);
        fadeTop = Math.Clamp(fadeTop, 0, height / 2f);
        fadeBottom = Math.Clamp(fadeBottom, 0, height / 2f);

        var centerW = width - fadeLeft - fadeRight;
        var centerH = height - fadeTop - fadeBottom;

        // 💡 使用 DrawingGroup 录制矢量图元，代替 Win2D 的 CanvasCommandList
        var drawingGroup = new DrawingGroup();
        using (var context = drawingGroup.Open())
        {
            // 纯色中心块
            if (centerW > 0 && centerH > 0)
            {
                context.DrawRectangle(Brushes.White, null, new Rect(fadeLeft, fadeTop, centerW, centerH));
            }

            // 顶部渐变
            if (fadeTop > 0 && centerW > 0)
            {
                var topBrush = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Absolute),
                    EndPoint = new RelativePoint(0, fadeTop, RelativeUnit.Absolute),
                    GradientStops = { new GradientStop(Colors.Transparent, 0), new GradientStop(Colors.White, 1) }
                };
                context.DrawRectangle(topBrush, null, new Rect(fadeLeft, 0, centerW, fadeTop));
            }

            // 底部渐变
            if (fadeBottom > 0 && centerW > 0)
            {
                var bottomBrush = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, height - fadeBottom, RelativeUnit.Absolute),
                    EndPoint = new RelativePoint(0, height, RelativeUnit.Absolute),
                    GradientStops = { new GradientStop(Colors.White, 0), new GradientStop(Colors.Transparent, 1) }
                };
                context.DrawRectangle(bottomBrush, null, new Rect(fadeLeft, height - fadeBottom, centerW, fadeBottom));
            }

            // 左侧渐变
            if (fadeLeft > 0 && centerH > 0)
            {
                var leftBrush = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Absolute),
                    EndPoint = new RelativePoint(fadeLeft, 0, RelativeUnit.Absolute),
                    GradientStops = { new GradientStop(Colors.Transparent, 0), new GradientStop(Colors.White, 1) }
                };
                context.DrawRectangle(leftBrush, null, new Rect(0, fadeTop, fadeLeft, centerH));
            }

            // 右侧渐变
            if (fadeRight > 0 && centerH > 0)
            {
                var rightBrush = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(width - fadeRight, 0, RelativeUnit.Absolute),
                    EndPoint = new RelativePoint(width, 0, RelativeUnit.Absolute),
                    GradientStops = { new GradientStop(Colors.White, 0), new GradientStop(Colors.Transparent, 1) }
                };
                context.DrawRectangle(rightBrush, null, new Rect(width - fadeRight, fadeTop, fadeRight, centerH));
            }

            // 四个角落的径向圆角羽化
            DrawCorner(context, 0, 0, fadeLeft, fadeTop, new Point(fadeLeft, fadeTop));
            DrawCorner(context, width - fadeRight, 0, fadeRight, fadeTop, new Point(width - fadeRight, fadeTop));
            DrawCorner(context, 0, height - fadeBottom, fadeLeft, fadeBottom, new Point(fadeLeft, height - fadeBottom));
            DrawCorner(context, width - fadeRight, height - fadeBottom, fadeRight, fadeBottom, new Point(width - fadeRight, height - fadeBottom));
        }

        // 💡 转换为带有平移偏移量的 DrawingBrush
        Brush = new DrawingBrush(drawingGroup)
        {
            AlignmentX = AlignmentX.Left,
            AlignmentY = AlignmentY.Top,
            Stretch = Stretch.None
        };

        // 更新缓存
        _currentMode = MaskRenderMode.EdgeFade;
        _lastBounds = bounds;
        _lastTop = fadeTop;
        _lastBottom = fadeBottom;
        _lastLeft = fadeLeft;
        _lastRight = fadeRight;
    }

    private void DrawCorner(DrawingContext context, float x, float y, float w, float h, Point center)
    {
        if (w <= 0 || h <= 0) return;

        // 💡 映射为 Avalonia 绝对像素模式的径向渐变画笔
        var radialBrush = new RadialGradientBrush
        {
            Center = new RelativePoint(center, RelativeUnit.Absolute),
            GradientOrigin = new RelativePoint(center, RelativeUnit.Absolute),
            RadiusX = new RelativeScalar(w, RelativeUnit.Absolute),
            RadiusY = new RelativeScalar(h, RelativeUnit.Absolute),
            GradientStops = { new GradientStop(Colors.White, 0), new GradientStop(Colors.Transparent, 1) }
        };

        context.DrawRectangle(radialBrush, null, new Rect(x, y, w, h));
    }

    /// <summary>
    /// 使用 GradientStop 数组自定义多个完全可见的区域和渐变
    /// </summary>
    public void Update(Rect bounds, GradientStop[] stops, bool isVertical = true)
    {
        // 缓存拦截检查
        if (_currentMode == MaskRenderMode.GradientStops &&
            Math.Abs(_lastBounds.X - bounds.X) < 0.1f && Math.Abs(_lastBounds.Y - bounds.Y) < 0.1f &&
            Math.Abs(_lastBounds.Width - bounds.Width) < 0.1f && Math.Abs(_lastBounds.Height - bounds.Height) < 0.1f &&
            _lastIsVertical == isVertical &&
            AreStopsEqual(_lastStops, stops) &&
            Brush != null)
            return;

        var width = (float)bounds.Width;
        var height = (float)bounds.Height;
        var startX = (float)bounds.X;
        var startY = (float)bounds.Y;

        var startPoint = new RelativePoint(0, 0, RelativeUnit.Absolute);
        var endPoint = isVertical
            ? new RelativePoint(0, height, RelativeUnit.Absolute)
            : new RelativePoint(width, 0, RelativeUnit.Absolute);

        // 💡 建立包含多节点的线性渐变画笔
        var multiStopBrush = new LinearGradientBrush
        {
            StartPoint = startPoint,
            EndPoint = endPoint,
            GradientStops = new()
        };
        multiStopBrush.GradientStops.AddRange(stops);

        var drawingGroup = new DrawingGroup();
        using (var context = drawingGroup.Open())
        {
            context.DrawRectangle(multiStopBrush, null, new Rect(0, 0, width, height));
        }

        Brush = new DrawingBrush(drawingGroup)
        {
            AlignmentX = AlignmentX.Left,
            AlignmentY = AlignmentY.Top,
            Stretch = Stretch.None
        };

        // 更新缓存
        _currentMode = MaskRenderMode.GradientStops;
        _lastBounds = bounds;
        _lastIsVertical = isVertical;
        _lastStops = stops.ToArray();
    }

    private bool AreStopsEqual(GradientStop[]? a, GradientStop[]? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++)
            if (Math.Abs(a[i].Offset - b[i].Offset) > 0.001f || a[i].Color != b[i].Color)
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