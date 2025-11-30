using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI;
using System;
using System.Linq;
using System.Numerics;
using Windows.UI;

namespace BetterLyrics.WinUI3.Renderer
{
    public partial class SpectrumRenderer : IDisposable
    {
        private CanvasGeometry? _spectrumGeometry;

        public void Draw(
            ICanvasResourceCreator resourceCreator,
            CanvasDrawingSession ds,
            float[]? spectrumData,
            int barCount,
            bool isEnabled,
            SpectrumPlacement placement,
            SpectrumStyle style,
            double canvasWidth,
            double canvasHeight,
            Color fillColor
            )
        {
            _spectrumGeometry?.Dispose();
            _spectrumGeometry = null;

            if (!isEnabled || spectrumData == null || spectrumData.Length == 0) return;

            _spectrumGeometry = CreateGeometry(resourceCreator, spectrumData, barCount, placement, style, canvasWidth, canvasHeight);

            if (_spectrumGeometry != null)
            {
                DrawGeometry(ds, _spectrumGeometry, fillColor, placement, canvasHeight);
            }
        }

        private CanvasGeometry? CreateGeometry(
            ICanvasResourceCreator creator,
            float[] data,
            int barCount,
            SpectrumPlacement placement,
            SpectrumStyle style,
            double width,
            double height)
        {
            if (barCount < 2) return null;

            float maxDataVal = 0;

            int checkCount = Math.Min(barCount, data.Length);
            for (int i = 0; i < checkCount; i++)
            {
                if (data[i] > maxDataVal) maxDataVal = data[i];
            }

            float limitY = (float)height * 0.2f; // 高度限制为总高度的 20%
            float scaleRatio = 1.0f;

            if (maxDataVal > limitY)
            {
                scaleRatio = limitY / maxDataVal;
            }

            using var pathBuilder = new CanvasPathBuilder(creator);

            if (style == SpectrumStyle.Bar)
            {
                float totalStep = (float)width / barCount;
                float gap = 2.0f;
                float barWidth = totalStep - gap;
                if (barWidth < 1.0f) { barWidth = totalStep; gap = 0f; }

                for (int i = 0; i < barCount; i++)
                {
                    float rawVal = i < data.Length ? data[i] : 0;
                    float barHeight = rawVal * scaleRatio;
                    if (barHeight < 0.5f) continue;

                    float x = i * totalStep;
                    float topY, bottomY;

                    if (placement == SpectrumPlacement.Top)
                    {
                        topY = 0;
                        bottomY = barHeight;
                    }
                    else // Bottom
                    {
                        topY = (float)height - barHeight;
                        bottomY = (float)height;
                    }

                    // 绘制独立矩形
                    pathBuilder.BeginFigure(new Vector2(x, topY));
                    pathBuilder.AddLine(new Vector2(x + barWidth, topY));
                    pathBuilder.AddLine(new Vector2(x + barWidth, bottomY));
                    pathBuilder.AddLine(new Vector2(x, bottomY));
                    pathBuilder.EndFigure(CanvasFigureLoop.Closed);
                }
            }
            else
            {
                var points = new Vector2[barCount];
                float pointSpacing = (float)width / (barCount - 1);

                for (int i = 0; i < barCount; i++)
                {
                    float rawVal = i < data.Length ? data[i] : 0;
                    float y = rawVal * scaleRatio;

                    // 处理翻转
                    if (placement == SpectrumPlacement.Bottom)
                    {
                        y = (float)height - y;
                    }

                    points[i] = new Vector2(i * pointSpacing, y);
                }

                // 绘制曲线
                pathBuilder.BeginFigure(points[0]);

                for (int i = 0; i < barCount - 1; i++)
                {
                    Vector2 p0 = points[Math.Max(i - 1, 0)];
                    Vector2 p1 = points[i];
                    Vector2 p2 = points[i + 1];
                    Vector2 p3 = points[Math.Min(i + 2, barCount - 1)];

                    Vector2 cp1 = p1 + (p2 - p0) / 6.0f;
                    Vector2 cp2 = p2 - (p3 - p1) / 6.0f;

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
                    pathBuilder.AddLine(new Vector2(points[barCount - 1].X, (float)height));
                    pathBuilder.AddLine(new Vector2(points[0].X, (float)height));
                }

                pathBuilder.EndFigure(CanvasFigureLoop.Closed);
            }

            return CanvasGeometry.CreatePath(pathBuilder);
        }

        private void DrawGeometry(
            CanvasDrawingSession ds,
            CanvasGeometry geometry,
            Color color,
            SpectrumPlacement placement,
            double height)
        {
            var stops = new CanvasGradientStop[]
            {
                new() { Position = 0.0f, Color = Colors.Transparent },
                new() { Position = 0.7f, Color = Colors.Transparent },
                new() { Position = 1.0f, Color = color }
            };

            using var brush = new CanvasLinearGradientBrush(ds, stops);

            if (placement == SpectrumPlacement.Top)
            {
                brush.StartPoint = new Vector2(0, (float)height);
                brush.EndPoint = new Vector2(0, 0);
            }
            else
            {
                brush.StartPoint = new Vector2(0, 0);
                brush.EndPoint = new Vector2(0, (float)height);
            }

            ds.FillGeometry(geometry, brush);
        }

        public void Dispose()
        {
            _spectrumGeometry?.Dispose();
            _spectrumGeometry = null;
        }
    }
}