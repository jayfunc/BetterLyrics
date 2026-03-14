using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.UI;
using System;
using System.Numerics;
using Windows.UI;

namespace BetterLyrics.WinUI3.Renderer
{
    public partial class EdgeFadeMaskRenderer : IDisposable
    {
        private CanvasCommandList? _maskCommandList;
        private CanvasImageBrush? _maskBrush;

        private float _lastWidth = -1;
        private float _lastHeight = -1;
        private float _lastTop = -1;
        private float _lastBottom = -1;
        private float _lastLeft = -1;
        private float _lastRight = -1;

        public CanvasImageBrush? Brush => _maskBrush;

        public void Update(ICanvasResourceCreator resourceCreator, float width, float height,
            float fadeLeft, float fadeTop, float fadeRight, float fadeBottom)
        {
            if (Math.Abs(_lastWidth - width) < 0.1f && Math.Abs(_lastHeight - height) < 0.1f &&
                Math.Abs(_lastTop - fadeTop) < 0.1f && Math.Abs(_lastBottom - fadeBottom) < 0.1f &&
                Math.Abs(_lastLeft - fadeLeft) < 0.1f && Math.Abs(_lastRight - fadeRight) < 0.1f &&
                _maskBrush != null)
            {
                return;
            }

            _maskBrush?.Dispose();
            _maskCommandList?.Dispose();

            _maskCommandList = new CanvasCommandList(resourceCreator);
            using (var ds = _maskCommandList.CreateDrawingSession())
            {
                ds.Clear(Color.FromArgb(0, 0, 0, 0));

                float centerW = width - fadeLeft - fadeRight;
                float centerH = height - fadeTop - fadeBottom;

                if (centerW > 0 && centerH > 0)
                {
                    ds.FillRectangle(fadeLeft, fadeTop, centerW, centerH, Colors.White);
                }

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

                DrawCorner(resourceCreator, ds, 0, 0, fadeLeft, fadeTop, new Vector2(fadeLeft, fadeTop)); // 左上
                DrawCorner(resourceCreator, ds, width - fadeRight, 0, fadeRight, fadeTop, new Vector2(width - fadeRight, fadeTop)); // 右上
                DrawCorner(resourceCreator, ds, 0, height - fadeBottom, fadeLeft, fadeBottom, new Vector2(fadeLeft, height - fadeBottom)); // 左下
                DrawCorner(resourceCreator, ds, width - fadeRight, height - fadeBottom, fadeRight, fadeBottom, new Vector2(width - fadeRight, height - fadeBottom)); // 右下
            }

            _maskBrush = new CanvasImageBrush(resourceCreator, _maskCommandList);
            _maskBrush.SourceRectangle = new(0, 0, width, height);

            _lastWidth = width; _lastHeight = height;
            _lastTop = fadeTop; _lastBottom = fadeBottom; _lastLeft = fadeLeft; _lastRight = fadeRight;
        }

        private void DrawCorner(ICanvasResourceCreator resourceCreator, CanvasDrawingSession ds,
                                float x, float y, float w, float h, Vector2 center)
        {
            if (w <= 0 || h <= 0) return;

            using var radialBrush = new CanvasRadialGradientBrush(resourceCreator, Colors.White, Colors.Transparent)
            {
                Center = center,
                RadiusX = w,
                RadiusY = h
            };
            ds.FillRectangle(x, y, w, h, radialBrush);
        }

        public void Dispose()
        {
            _maskBrush?.Dispose();
            _maskCommandList?.Dispose();
        }
    }
}
