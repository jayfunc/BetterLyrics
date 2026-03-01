using BetterLyrics.WinUI3.Enums;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI;
using System;
using System.Numerics;
using Windows.UI;
using Windows.Foundation;
using BetterLyrics.WinUI3.Models;

namespace BetterLyrics.WinUI3.Renderer
{
    public partial class SpectrumRenderer : BreathingRendererBase, IDisposable
    {
        private CanvasGeometry? _spectrumGeometry;

        public void Draw(
            ICanvasResourceCreator resourceCreator,
            CanvasDrawingSession ds,
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
            float cornerRadiusPercentage
            )
        {
            _spectrumGeometry?.Dispose();
            _spectrumGeometry = null;

            if (!isEnabled || spectrumData == null || spectrumData.Length == 0) return;

            _spectrumGeometry = CreateGeometry(resourceCreator, spectrumData, barCount, placement, style, canvasWidth, canvasHeight, albumRect, cornerRadiusPercentage);

            if (_spectrumGeometry != null)
            {
                var center = placement == SpectrumPlacement.AroundAlbumArt
                    ? new Vector2((float)(albumRect.X + albumRect.Width / 2), (float)(albumRect.Y + albumRect.Height / 2))
                    : new Vector2((float)canvasWidth / 2, placement == SpectrumPlacement.Bottom ? (float)canvasHeight : 0);

                ApplyBreathingTransform(ds, center, isBreathingEffectEnabled);

                DrawGeometry(ds, _spectrumGeometry, fillColor, isGlowEffectEnabled, opacity, placement, style, canvasHeight, albumRect);

                ResetTransform(ds, isBreathingEffectEnabled);
            }
        }

        public void Update(float bassEnergy, int breathingIntensity)
        {
            base.UpdateBreathing(bassEnergy, breathingIntensity);
        }

        private CanvasGeometry? CreateGeometry(
            ICanvasResourceCreator creator,
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

            float viewHeight = (float)height;

            float fixedScaleFactor = 0.05f * viewHeight;

            using var pathBuilder = new CanvasPathBuilder(creator);

            if (placement == SpectrumPlacement.AroundAlbumArt)
            {
                float w = (float)albumRect.Width;
                float h = (float)albumRect.Height;
                float cornerRadius = cornerRadiusPercentage / 100f * Math.Min(w / 2, h / 2);
                float r = cornerRadius;

                float perimeter = 2 * (w - 2 * r) + 2 * (h - 2 * r) + (float)(2 * Math.PI * r);
                float step = perimeter / barCount;

                Span<Vector2> outerPoints = barCount <= 512
                    ? stackalloc Vector2[barCount]
                    : new Vector2[barCount];

                for (int i = 0; i < barCount; i++)
                {
                    float rawVal = i < data.Length ? data[i] : 0;

                    float barHeight = rawVal * fixedScaleFactor * 2.0f;

                    float distance = (i * step) % perimeter;

                    var (pos, normal) = GetPointAndNormalOnRoundRect(distance, albumRect, r);

                    outerPoints[i] = pos + normal * barHeight;
                }

                pathBuilder.BeginFigure(outerPoints[0]);

                for (int i = 0; i < barCount; i++)
                {
                    Vector2 p0 = outerPoints[(i - 1 + barCount) % barCount];
                    Vector2 p1 = outerPoints[i];
                    Vector2 p2 = outerPoints[(i + 1) % barCount];
                    Vector2 p3 = outerPoints[(i + 2) % barCount];

                    Vector2 cp1 = p1 + (p2 - p0) * 0.1666f;
                    Vector2 cp2 = p2 - (p3 - p1) * 0.1666f;

                    pathBuilder.AddCubicBezier(cp1, cp2, p2);
                }

                pathBuilder.EndFigure(CanvasFigureLoop.Closed);
            }
            else
            {
                if (style == SpectrumStyle.Bar)
                {
                    float totalStep = (float)width / barCount;
                    float gap = 2.0f;
                    float barWidth = totalStep - gap;
                    if (barWidth < 1.0f)
                    {
                        barWidth = totalStep;
                        gap = 0f;
                    }
                    float halfGap = gap / 2.0f;

                    for (int i = 0; i < barCount; i++)
                    {
                        float rawVal = i < data.Length ? data[i] : 0;

                        float barHeight = rawVal * fixedScaleFactor;

                        if (barHeight > viewHeight) barHeight = viewHeight;

                        if (barHeight < 1.0f) continue;

                        float x = i * totalStep + halfGap;
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

                        pathBuilder.BeginFigure(new Vector2(x, topY));
                        pathBuilder.AddLine(new Vector2(x + barWidth, topY));
                        pathBuilder.AddLine(new Vector2(x + barWidth, bottomY));
                        pathBuilder.AddLine(new Vector2(x, bottomY));
                        pathBuilder.EndFigure(CanvasFigureLoop.Closed);
                    }
                }
                else // Curve
                {
                    Span<Vector2> points = barCount <= 512
                        ? stackalloc Vector2[barCount]
                        : new Vector2[barCount];

                    float pointSpacing = (float)width / (barCount - 1);

                    for (int i = 0; i < barCount; i++)
                    {
                        float rawVal = i < data.Length ? data[i] : 0;
                        float yVal = rawVal * fixedScaleFactor;

                        if (yVal > viewHeight) yVal = viewHeight;

                        float y = (placement == SpectrumPlacement.Bottom)
                            ? viewHeight - yVal
                            : yVal;

                        points[i] = new Vector2(i * pointSpacing, y);
                    }

                    pathBuilder.BeginFigure(points[0]);

                    for (int i = 0; i < barCount - 1; i++)
                    {
                        Vector2 p0 = points[i > 0 ? i - 1 : 0];
                        Vector2 p1 = points[i];
                        Vector2 p2 = points[i + 1];
                        Vector2 p3 = points[i + 2 < barCount ? i + 2 : barCount - 1];

                        Vector2 cp1 = p1 + (p2 - p0) * 0.1666f;
                        Vector2 cp2 = p2 - (p3 - p1) * 0.1666f;

                        pathBuilder.AddCubicBezier(cp1, cp2, p2);
                    }

                    // 封口
                    if (placement == SpectrumPlacement.Top)
                    {
                        pathBuilder.AddLine(new Vector2(points[barCount - 1].X, 0));
                        pathBuilder.AddLine(new Vector2(points[0].X, 0));
                    }
                    else
                    {
                        pathBuilder.AddLine(new Vector2(points[barCount - 1].X, viewHeight));
                        pathBuilder.AddLine(new Vector2(points[0].X, viewHeight));
                    }

                    pathBuilder.EndFigure(CanvasFigureLoop.Closed);
                }
            }

            return CanvasGeometry.CreatePath(pathBuilder);
        }

        private (Vector2 Position, Vector2 Normal) GetPointAndNormalOnRoundRect(float distance, Rect rect, float r)
        {
            float w = (float)rect.Width;
            float h = (float)rect.Height;
            float x = (float)rect.X;
            float y = (float)rect.Y;

            float topL = w - 2 * r;
            float arcL = (float)(Math.PI * r / 2.0);
            float rightL = h - 2 * r;

            // 上边缘 (向右)
            if (distance <= topL)
                return (new Vector2(x + r + distance, y), new Vector2(0, -1));
            distance -= topL;

            // 右上角圆弧
            if (distance <= arcL)
            {
                float angle = -MathF.PI / 2 + (distance / arcL) * (MathF.PI / 2); // -90度到0度
                Vector2 n = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                return (new Vector2(x + w - r, y + r) + n * r, n);
            }
            distance -= arcL;

            // 右边缘 (向下)
            if (distance <= rightL)
                return (new Vector2(x + w, y + r + distance), new Vector2(1, 0));
            distance -= rightL;

            // 右下角圆弧
            if (distance <= arcL)
            {
                float angle = 0 + (distance / arcL) * (MathF.PI / 2); // 0度到90度
                Vector2 n = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                return (new Vector2(x + w - r, y + h - r) + n * r, n);
            }
            distance -= arcL;

            // 下边缘 (向左)
            if (distance <= topL)
                return (new Vector2(x + w - r - distance, y + h), new Vector2(0, 1));
            distance -= topL;

            // 左下角圆弧
            if (distance <= arcL)
            {
                float angle = MathF.PI / 2 + (distance / arcL) * (MathF.PI / 2); // 90度到180度
                Vector2 n = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
                return (new Vector2(x + r, y + h - r) + n * r, n);
            }
            distance -= arcL;

            // 左边缘 (向上)
            if (distance <= rightL)
                return (new Vector2(x, y + h - r - distance), new Vector2(-1, 0));
            distance -= rightL;

            // 左上角圆弧
            float finalAngle = MathF.PI + (distance / arcL) * (MathF.PI / 2); // 180度到270度
            Vector2 finalN = new Vector2(MathF.Cos(finalAngle), MathF.Sin(finalAngle));
            return (new Vector2(x + r, y + r) + finalN * r, finalN);
        }

        private static void DrawGeometry(
            CanvasDrawingSession ds,
            CanvasGeometry geometry,
            Color color,
            bool isGlowEffectEnabled,
            float opacity,
            SpectrumPlacement placement,
            SpectrumStyle style,
            double height,
            Rect albumRect)
        {
            var stops = new CanvasGradientStop[]
            {
                new() { Position = 0.0f, Color = Colors.Transparent },
                new() { Position = 1.0f, Color = Color.FromArgb((byte)(255 * opacity), color.R, color.G, color.B) }
            };

            ICanvasBrush brush;

            if (placement == SpectrumPlacement.AroundAlbumArt)
            {
                float centerX = (float)(albumRect.X + albumRect.Width / 2);
                float centerY = (float)(albumRect.Y + albumRect.Height / 2);

                float maxRadius = (float)(Math.Max(albumRect.Width, albumRect.Height) / 2.0 + height * 0.3);

                float edgeRatio = (float)(Math.Min(albumRect.Width, albumRect.Height) / 2.0) / maxRadius;
                edgeRatio = Math.Clamp(edgeRatio, 0.1f, 0.8f);

                var roundStops = new CanvasGradientStop[]
                {
                    new() { Position = 0.0f, Color = Color.FromArgb((byte)(255 * opacity), color.R, color.G, color.B) },
                    new() { Position = edgeRatio, Color = Color.FromArgb((byte)(255 * opacity), color.R, color.G, color.B) },

                    new() { Position = 1.0f, Color = Colors.Transparent }
                };

                brush = new CanvasRadialGradientBrush(ds, roundStops)
                {
                    Center = new Vector2(centerX, centerY),
                    RadiusX = maxRadius,
                    RadiusY = maxRadius
                };
            }
            else
            {
                var linearBrush = new CanvasLinearGradientBrush(ds, stops);
                if (placement == SpectrumPlacement.Top)
                {
                    linearBrush.StartPoint = new Vector2(0, (float)height);
                    linearBrush.EndPoint = new Vector2(0, 0);
                }
                else
                {
                    linearBrush.StartPoint = new Vector2(0, 0);
                    linearBrush.EndPoint = new Vector2(0, (float)height);
                }
                brush = linearBrush;
            }

            if (isGlowEffectEnabled)
            {
                // 辉光层
                using var commandList = new CanvasCommandList(ds);
                using (var clds = commandList.CreateDrawingSession())
                {
                    clds.FillGeometry(geometry, brush);
                }

                using var blurEffect = new GaussianBlurEffect
                {
                    Source = commandList,
                    BlurAmount = 16.0f,
                    BorderMode = EffectBorderMode.Soft
                };

                // 向外发射辉光
                float glowOffsetY = placement == SpectrumPlacement.AroundAlbumArt ? 0 : (placement == SpectrumPlacement.Bottom ? -4.0f : 4.0f);

                using (var layer = ds.CreateLayer(1.0f))
                {
                    // 让颜色叠加变亮
                    ds.Blend = CanvasBlend.Add;
                    ds.DrawImage(blurEffect, 0, glowOffsetY);
                    ds.Blend = CanvasBlend.SourceOver; // 还原混合模式
                }
            }

            ds.FillGeometry(geometry, brush);

            // 绘制一条高亮的描边，增强轮廓感，让波峰更清晰
            //ds.DrawGeometry(geometry, Colors.White, 1.0f);

            brush.Dispose();
        }

        public void Dispose()
        {
            _spectrumGeometry?.Dispose();
            _spectrumGeometry = null;
        }
    }
}