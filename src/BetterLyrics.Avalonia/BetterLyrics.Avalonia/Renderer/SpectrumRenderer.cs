using System;
using System.Numerics;
using Avalonia;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Rendering.SceneGraph;
using Avalonia.Skia;
using BetterLyrics.Core.Enums;
using SkiaSharp;

namespace BetterLyrics.Avalonia.Renderer;

public partial class SpectrumRenderer : EffectRendererBase, IDisposable
{
    private SKPath? _spectrumPath;

    public void Dispose()
    {
        _spectrumPath?.Dispose();
        _spectrumPath = null;
    }

    public void Draw(
        DrawingContext context,
        float[]? spectrumData,
        int barCount,
        bool isEnabled,
        bool isGlowEffectEnabled,
        bool isBreathingEffectEnabled,
        float opacity,
        SpectrumPlacement placement,
        SpectrumStyle style,
        double canvasWidth,
        double canvasHeight,
        Color fillColor,
        Rect albumRect,
        float cornerRadiusPercentage)
    {
        _spectrumPath?.Dispose();
        _spectrumPath = null;

        if (!isEnabled || spectrumData == null || spectrumData.Length == 0) return;

        // Generate the SKPath geometry
        _spectrumPath = CreatePath(spectrumData, barCount, placement, style, canvasWidth,
            canvasHeight, albumRect, cornerRadiusPercentage);

        if (_spectrumPath != null)
        {
            var bounds = new Rect(0, 0, canvasWidth, canvasHeight);

            // Queue the custom draw operation
            context.Custom(new SpectrumCustomDrawOperation(
                this, bounds, center: GetCenter(placement, canvasWidth, canvasHeight, albumRect),
                isBreathingEffectEnabled, fillColor, isGlowEffectEnabled, opacity, placement, style, canvasHeight, albumRect));
        }
    }

    public void Update(
        Size controlSize,
        SpectrumPlacement placement,
        Rect albumRect,
        float bassEnergy,
        int breathingIntensity,
        bool is3DEnabled)
    {
        UpdateBreathing(bassEnergy, breathingIntensity);

        if (is3DEnabled)
        {
            var trueCenter2D = GetCenter(placement, controlSize.Width, controlSize.Height, albumRect);
            var center3D = new Vector3(trueCenter2D.X, trueCenter2D.Y, 0);

            UpdateParallaxMatrix(center3D, true);
        }
        else
        {
            ResetParallaxMatrix();
        }
    }

    private static Vector2 GetCenter(SpectrumPlacement placement, double canvasWidth, double canvasHeight, Rect albumRect)
    {
        return placement == SpectrumPlacement.AroundAlbumArt
            ? new Vector2((float)(albumRect.X + albumRect.Width / 2), (float)(albumRect.Y + albumRect.Height / 2))
            : new Vector2((float)canvasWidth / 2, placement == SpectrumPlacement.Bottom ? (float)canvasHeight : 0);
    }

    private void RenderSkia(
        SKCanvas canvas,
        Vector2 center,
        bool isBreathingEffectEnabled,
        Color fillColor,
        bool isGlowEffectEnabled,
        float opacity,
        SpectrumPlacement placement,
        SpectrumStyle style,
        double canvasHeight,
        Rect albumRect)
    {
        if (_spectrumPath == null) return;

        canvas.Save();

        // 1. Apply Parallax 3D Matrix (Inherited from EffectRendererBase)
        if (!_threeDimMatrix.IsIdentity)
        {
            // Note: Make sure ToSKMatrix is available in your EffectRendererBase from the previous step
            canvas.Concat(ToSKMatrix(_threeDimMatrix));
        }

        // 2. Apply Breathing Transform (2D Scale)
        ApplyBreathingTransform(canvas, center, isBreathingEffectEnabled);

        // 3. Draw Geometry
        DrawSkiaGeometry(canvas, _spectrumPath, fillColor, isGlowEffectEnabled, opacity, placement, style, canvasHeight, albumRect);

        canvas.Restore();
    }

    private SKPath? CreatePath(
        float[] data,
        int barCount,
        SpectrumPlacement placement,
        SpectrumStyle style,
        double width,
        double height,
        Rect albumRect,
        float cornerRadiusPercentage)
    {
        if (barCount < 2 || data == null || data.Length == 0) return null;

        var viewHeight = (float)height;
        var fixedScaleFactor = 0.05f * viewHeight;
        var path = new SKPath();

        if (placement == SpectrumPlacement.AroundAlbumArt)
        {
            var w = (float)albumRect.Width;
            var h = (float)albumRect.Height;
            var cornerRadius = cornerRadiusPercentage / 100f * Math.Min(w / 2, h / 2);
            var r = cornerRadius;

            var perimeter = 2 * (w - 2 * r) + 2 * (h - 2 * r) + (float)(2 * Math.PI * r);
            var step = perimeter / barCount;

            var outerPoints = barCount <= 512 ? stackalloc Vector2[barCount] : new Vector2[barCount];

            for (var i = 0; i < barCount; i++)
            {
                var rawVal = i < data.Length ? data[i] : 0;
                var barHeight = rawVal * fixedScaleFactor * 2.0f;
                var distance = i * step % perimeter;
                var (pos, normal) = GetPointAndNormalOnRoundRect(distance, albumRect, r);

                outerPoints[i] = pos + normal * barHeight;
            }

            path.MoveTo(outerPoints[0].X, outerPoints[0].Y);

            for (var i = 0; i < barCount; i++)
            {
                var p0 = outerPoints[(i - 1 + barCount) % barCount];
                var p1 = outerPoints[i];
                var p2 = outerPoints[(i + 1) % barCount];
                var p3 = outerPoints[(i + 2) % barCount];

                var cp1 = p1 + (p2 - p0) * 0.1666f;
                var cp2 = p2 - (p3 - p1) * 0.1666f;

                path.CubicTo(cp1.X, cp1.Y, cp2.X, cp2.Y, p2.X, p2.Y);
            }

            path.Close();
        }
        else
        {
            if (style == SpectrumStyle.Bar)
            {
                var totalStep = (float)width / barCount;
                var gap = 2.0f;
                var barWidth = totalStep - gap;
                if (barWidth < 1.0f)
                {
                    barWidth = totalStep;
                    gap = 0f;
                }

                var halfGap = gap / 2.0f;

                for (var i = 0; i < barCount; i++)
                {
                    var rawVal = i < data.Length ? data[i] : 0;
                    var barHeight = rawVal * fixedScaleFactor;

                    if (barHeight > viewHeight) barHeight = viewHeight;
                    if (barHeight < 1.0f) continue;

                    var x = i * totalStep + halfGap;
                    float topY, bottomY;

                    if (placement == SpectrumPlacement.Top)
                    {
                        topY = 0;
                        bottomY = barHeight;
                    }
                    else // Bottom
                    {
                        topY = viewHeight - barHeight;
                        bottomY = viewHeight;
                    }

                    path.MoveTo(x, topY);
                    path.LineTo(x + barWidth, topY);
                    path.LineTo(x + barWidth, bottomY);
                    path.LineTo(x, bottomY);
                    path.Close();
                }
            }
            else // Curve
            {
                var points = barCount <= 512 ? stackalloc Vector2[barCount] : new Vector2[barCount];
                var pointSpacing = (float)width / (barCount - 1);

                for (var i = 0; i < barCount; i++)
                {
                    var rawVal = i < data.Length ? data[i] : 0;
                    var yVal = rawVal * fixedScaleFactor;

                    if (yVal > viewHeight) yVal = viewHeight;

                    var y = placement == SpectrumPlacement.Bottom ? viewHeight - yVal : yVal;
                    points[i] = new Vector2(i * pointSpacing, y);
                }

                path.MoveTo(points[0].X, points[0].Y);

                for (var i = 0; i < barCount - 1; i++)
                {
                    var p0 = points[i > 0 ? i - 1 : 0];
                    var p1 = points[i];
                    var p2 = points[i + 1];
                    var p3 = points[i + 2 < barCount ? i + 2 : barCount - 1];

                    var cp1 = p1 + (p2 - p0) * 0.1666f;
                    var cp2 = p2 - (p3 - p1) * 0.1666f;

                    path.CubicTo(cp1.X, cp1.Y, cp2.X, cp2.Y, p2.X, p2.Y);
                }

                if (placement == SpectrumPlacement.Top)
                {
                    path.LineTo(points[barCount - 1].X, 0);
                    path.LineTo(points[0].X, 0);
                }
                else
                {
                    path.LineTo(points[barCount - 1].X, viewHeight);
                    path.LineTo(points[0].X, viewHeight);
                }

                path.Close();
            }
        }

        return path;
    }

    private static (Vector2 Position, Vector2 Normal) GetPointAndNormalOnRoundRect(float distance, Rect rect, float r)
    {
        // Remains structurally identical to your Win2D implementation.
        // It outputs generic System.Numerics.Vector2, making it platform agnostic.
        var w = (float)rect.Width;
        var h = (float)rect.Height;
        var x = (float)rect.X;
        var y = (float)rect.Y;

        var topL = w - 2 * r;
        var arcL = (float)(Math.PI * r / 2.0);
        var rightL = h - 2 * r;

        if (distance <= topL) return (new Vector2(x + r + distance, y), new Vector2(0, -1));
        distance -= topL;

        if (distance <= arcL)
        {
            var angle = -MathF.PI / 2 + distance / arcL * (MathF.PI / 2);
            var n = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            return (new Vector2(x + w - r, y + r) + n * r, n);
        }
        distance -= arcL;

        if (distance <= rightL) return (new Vector2(x + w, y + r + distance), new Vector2(1, 0));
        distance -= rightL;

        if (distance <= arcL)
        {
            var angle = distance / arcL * (MathF.PI / 2);
            var n = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            return (new Vector2(x + w - r, y + h - r) + n * r, n);
        }
        distance -= arcL;

        if (distance <= topL) return (new Vector2(x + w - r - distance, y + h), new Vector2(0, 1));
        distance -= topL;

        if (distance <= arcL)
        {
            var angle = MathF.PI / 2 + distance / arcL * (MathF.PI / 2);
            var n = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            return (new Vector2(x + r, y + h - r) + n * r, n);
        }
        distance -= arcL;

        if (distance <= rightL) return (new Vector2(x, y + h - r - distance), new Vector2(-1, 0));
        distance -= rightL;

        var finalAngle = MathF.PI + distance / arcL * (MathF.PI / 2);
        var finalN = new Vector2(MathF.Cos(finalAngle), MathF.Sin(finalAngle));
        return (new Vector2(x + r, y + r) + finalN * r, finalN);
    }

    private static void DrawSkiaGeometry(
        SKCanvas canvas,
        SKPath path,
        Color color,
        bool isGlowEffectEnabled,
        float opacity,
        SpectrumPlacement placement,
        SpectrumStyle style,
        double height,
        Rect albumRect)
    {
        SKShader shader;
        byte alpha = (byte)(255 * opacity);
        var skBaseColor = new SKColor(color.R, color.G, color.B, alpha);

        if (placement == SpectrumPlacement.AroundAlbumArt)
        {
            var centerX = (float)(albumRect.X + albumRect.Width / 2);
            var centerY = (float)(albumRect.Y + albumRect.Height / 2);
            var maxRadius = (float)(Math.Max(albumRect.Width, albumRect.Height) / 2.0 + height * 0.3);

            var edgeRatio = (float)(Math.Min(albumRect.Width, albumRect.Height) / 2.0) / maxRadius;
            edgeRatio = Math.Clamp(edgeRatio, 0.1f, 0.8f);

            var colors = new[] { skBaseColor, skBaseColor, SKColors.Transparent };
            var positions = new[] { 0.0f, edgeRatio, 1.0f };

            shader = SKShader.CreateRadialGradient(
                new SKPoint(centerX, centerY), maxRadius, colors, positions, SKShaderTileMode.Clamp);
        }
        else
        {
            var colors = new[] { SKColors.Transparent, skBaseColor };
            var positions = new[] { 0.0f, 1.0f };

            var start = placement == SpectrumPlacement.Top ? new SKPoint(0, (float)height) : new SKPoint(0, 0);
            var end = placement == SpectrumPlacement.Top ? new SKPoint(0, 0) : new SKPoint(0, (float)height);

            shader = SKShader.CreateLinearGradient(start, end, colors, positions, SKShaderTileMode.Clamp);
        }

        if (isGlowEffectEnabled)
        {
            var glowOffsetY = placement == SpectrumPlacement.AroundAlbumArt ? 0 :
                              placement == SpectrumPlacement.Bottom ? -4.0f : 4.0f;

            // In Skia, a blur radius is generally (sigma * 3). Win2D BlurAmount of 16 maps to roughly ~5.3 sigma.
            using var blurFilter = SKImageFilter.CreateBlur(5.3f, 5.3f);
            using var blurPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Shader = shader,
                ImageFilter = blurFilter,
                BlendMode = SKBlendMode.Plus, // Equivalent to CanvasBlend.Add
                IsAntialias = true
            };

            canvas.Save();
            canvas.Translate(0, glowOffsetY);
            canvas.DrawPath(path, blurPaint);
            canvas.Restore();
        }

        using var normalPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Shader = shader,
            IsAntialias = true
        };

        canvas.DrawPath(path, normalPaint);
        shader.Dispose();
    }

    // --- Skia Custom Draw Operation Adapter ---
    private sealed class SpectrumCustomDrawOperation : ICustomDrawOperation
    {
        private readonly SpectrumRenderer _renderer;
        private readonly Vector2 _center;
        private readonly bool _isBreathingEnabled;
        private readonly Color _fillColor;
        private readonly bool _isGlowEffectEnabled;
        private readonly float _opacity;
        private readonly SpectrumPlacement _placement;
        private readonly SpectrumStyle _style;
        private readonly double _canvasHeight;
        private readonly Rect _albumRect;

        public Rect Bounds { get; }

        public SpectrumCustomDrawOperation(SpectrumRenderer renderer, Rect bounds, Vector2 center,
            bool isBreathingEnabled, Color fillColor, bool isGlowEffectEnabled, float opacity,
            SpectrumPlacement placement, SpectrumStyle style, double canvasHeight, Rect albumRect)
        {
            _renderer = renderer;
            Bounds = bounds;
            _center = center;
            _isBreathingEnabled = isBreathingEnabled;
            _fillColor = fillColor;
            _isGlowEffectEnabled = isGlowEffectEnabled;
            _opacity = opacity;
            _placement = placement;
            _style = style;
            _canvasHeight = canvasHeight;
            _albumRect = albumRect;
        }

        public void Dispose() { }
        public bool HitTest(Point p) => false;
        public bool Equals(ICustomDrawOperation? other) => false;

        public void Render(ImmediateDrawingContext context)
        {
            var leaseFeature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if (leaseFeature == null) return;

            using var lease = leaseFeature.Lease();
            _renderer.RenderSkia(lease.SkCanvas, _center, _isBreathingEnabled, _fillColor,
                _isGlowEffectEnabled, _opacity, _placement, _style, _canvasHeight, _albumRect);
        }
    }
}