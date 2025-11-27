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
    public class SpectrumRenderer : IDisposable
    {
        private CanvasGeometry? _spectrumGeometry;

        public void Draw(
            ICanvasResourceCreator resourceCreator,
            CanvasDrawingSession ds,
            float[]? spectrumData,
            int barCount,
            bool isEnabled,
            SpectrumPlacement placement,
            double canvasWidth,
            double canvasHeight,
            Color fillColor
            )
        {
            _spectrumGeometry?.Dispose();
            _spectrumGeometry = null;

            if (!isEnabled || spectrumData == null || spectrumData.Length == 0) return;

            _spectrumGeometry = CreateGeometry(resourceCreator, spectrumData, barCount, placement, canvasWidth, canvasHeight);

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
            double width,
            double height)
        {
            if (barCount < 2) return null;

            var points = new Vector2[barCount];
            float pointSpacing = (float)width / (barCount - 1);

            for (int i = 0; i < barCount; i++)
            {
                float val = i < data.Length ? data[i] : 0;
                points[i] = new Vector2(i * pointSpacing, val);
            }

            // 限制高度
            float maxY = 0;
            foreach (var p in points) if (p.Y > maxY) maxY = p.Y;

            float limitY = (float)height * 0.2f;
            if (maxY > limitY)
            {
                float ratio = limitY / maxY;
                for (int i = 0; i < points.Length; i++) points[i].Y *= ratio;
            }

            // 翻转 Y 轴
            if (placement == SpectrumPlacement.Bottom)
            {
                for (int i = 0; i < points.Length; i++)
                {
                    points[i].Y = (float)height - points[i].Y;
                }
            }

            using var pathBuilder = new CanvasPathBuilder(creator);
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