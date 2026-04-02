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

        // 缓存上一次的状态，减少不必要的重绘
        private Windows.Foundation.Rect _lastBounds = new Windows.Foundation.Rect(0, 0, 0, 0);
        private float _lastLeft = 0;
        private float _lastTop = 0;
        private float _lastRight = 0;
        private float _lastBottom = 0;

        public CanvasImageBrush? Brush => _maskBrush;

        public void Update(ICanvasResourceCreator resourceCreator, float width, float height,
            float fadeLeftPercentage, float fadeTopPercentage, float fadeRightPercentage, float fadeBottomPercentage)
        {
            float fadeLeft = (fadeLeftPercentage / 100f) * (width / 2f);
            float fadeTop = (fadeTopPercentage / 100f) * (height / 2f);
            float fadeRight = (fadeRightPercentage / 100f) * (width / 2f);
            float fadeBottom = (fadeBottomPercentage / 100f) * (height / 2f);

            var bounds = new Windows.Foundation.Rect(0, 0, width, height);
            UpdateCore(resourceCreator, bounds, fadeLeft, fadeTop, fadeRight, fadeBottom);
        }

        public void Update(ICanvasResourceCreator resourceCreator, Windows.Foundation.Rect targetRect, float fadeWidth)
        {
            UpdateCore(resourceCreator, targetRect, fadeWidth, fadeWidth, fadeWidth, fadeWidth);
        }

        public void Update(ICanvasResourceCreator resourceCreator, Windows.Foundation.Rect targetRect,
            float fadeLeft, float fadeTop, float fadeRight, float fadeBottom)
        {
            UpdateCore(resourceCreator, targetRect, fadeLeft, fadeTop, fadeRight, fadeBottom);
        }

        private void UpdateCore(ICanvasResourceCreator resourceCreator, Windows.Foundation.Rect bounds,
            float fadeLeft, float fadeTop, float fadeRight, float fadeBottom)
        {
            // 参数完全一致则跳过渲染
            if (Math.Abs(_lastBounds.X - bounds.X) < 0.1f && Math.Abs(_lastBounds.Y - bounds.Y) < 0.1f &&
                Math.Abs(_lastBounds.Width - bounds.Width) < 0.1f && Math.Abs(_lastBounds.Height - bounds.Height) < 0.1f &&
                Math.Abs(_lastTop - fadeTop) < 0.1f && Math.Abs(_lastBottom - fadeBottom) < 0.1f &&
                Math.Abs(_lastLeft - fadeLeft) < 0.1f && Math.Abs(_lastRight - fadeRight) < 0.1f &&
                _maskBrush != null)
            {
                return;
            }

            _maskBrush?.Dispose();
            _maskCommandList?.Dispose();

            _maskCommandList = new CanvasCommandList(resourceCreator);

            // 获取宽高
            float width = (float)bounds.Width;
            float height = (float)bounds.Height;
            float startX = (float)bounds.X;
            float startY = (float)bounds.Y;

            using (var ds = _maskCommandList.CreateDrawingSession())
            {
                ds.Clear(Color.FromArgb(0, 0, 0, 0));

                // 内部绘制基于 0, 0
                fadeLeft = Math.Clamp(fadeLeft, 0, width / 2f);
                fadeRight = Math.Clamp(fadeRight, 0, width / 2f);
                fadeTop = Math.Clamp(fadeTop, 0, height / 2f);
                fadeBottom = Math.Clamp(fadeBottom, 0, height / 2f);

                float centerW = width - fadeLeft - fadeRight;
                float centerH = height - fadeTop - fadeBottom;

                // 中心实心区域（从 fadeLeft, fadeTop 开始画）
                if (centerW > 0 && centerH > 0)
                {
                    ds.FillRectangle(fadeLeft, fadeTop, centerW, centerH, Colors.White);
                }

                // 顶部渐变（从 0 开始）
                if (fadeTop > 0 && centerW > 0)
                {
                    using var topBrush = new CanvasLinearGradientBrush(resourceCreator, Colors.Transparent, Colors.White)
                    { StartPoint = new Vector2(0, 0), EndPoint = new Vector2(0, fadeTop) };
                    ds.FillRectangle(fadeLeft, 0, centerW, fadeTop, topBrush);
                }

                // 底部渐变
                if (fadeBottom > 0 && centerW > 0)
                {
                    using var bottomBrush = new CanvasLinearGradientBrush(resourceCreator, Colors.White, Colors.Transparent)
                    { StartPoint = new Vector2(0, height - fadeBottom), EndPoint = new Vector2(0, height) };
                    ds.FillRectangle(fadeLeft, height - fadeBottom, centerW, fadeBottom, bottomBrush);
                }

                // 左侧渐变
                if (fadeLeft > 0 && centerH > 0)
                {
                    using var leftBrush = new CanvasLinearGradientBrush(resourceCreator, Colors.Transparent, Colors.White)
                    { StartPoint = new Vector2(0, 0), EndPoint = new Vector2(fadeLeft, 0) };
                    ds.FillRectangle(0, fadeTop, fadeLeft, centerH, leftBrush);
                }

                // 右侧渐变
                if (fadeRight > 0 && centerH > 0)
                {
                    using var rightBrush = new CanvasLinearGradientBrush(resourceCreator, Colors.White, Colors.Transparent)
                    { StartPoint = new Vector2(width - fadeRight, 0), EndPoint = new Vector2(width, 0) };
                    ds.FillRectangle(width - fadeRight, fadeTop, fadeRight, centerH, rightBrush);
                }

                // 四个角绘制（坐标基于 0, 0）
                DrawCorner(resourceCreator, ds, 0, 0, fadeLeft, fadeTop, new Vector2(fadeLeft, fadeTop));
                DrawCorner(resourceCreator, ds, width - fadeRight, 0, fadeRight, fadeTop, new Vector2(width - fadeRight, fadeTop));
                DrawCorner(resourceCreator, ds, 0, height - fadeBottom, fadeLeft, fadeBottom, new Vector2(fadeLeft, height - fadeBottom));
                DrawCorner(resourceCreator, ds, width - fadeRight, height - fadeBottom, fadeRight, fadeBottom, new Vector2(width - fadeRight, height - fadeBottom));
            }

            _maskBrush = new CanvasImageBrush(resourceCreator, _maskCommandList)
            {
                SourceRectangle = new Windows.Foundation.Rect(0, 0, width, height),
                Transform = Matrix3x2.CreateTranslation(startX, startY)
            };

            // 更新缓存
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