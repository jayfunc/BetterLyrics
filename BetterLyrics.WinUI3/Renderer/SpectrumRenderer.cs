using BetterLyrics.WinUI3.Enums;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI;
using System;
using System.Numerics;
using Windows.UI;

namespace BetterLyrics.WinUI3.Renderer
{
    public partial class SpectrumRenderer : IDisposable
    {
        private float _breathingScale = 1.0f;
        private float _targetBreathingScale = 1.0f;

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
            Color fillColor
            )
        {
            _spectrumGeometry?.Dispose();
            _spectrumGeometry = null;

            if (!isEnabled || spectrumData == null || spectrumData.Length == 0) return;

            _spectrumGeometry = CreateGeometry(resourceCreator, spectrumData, barCount, placement, style, canvasWidth, canvasHeight);

            if (_spectrumGeometry != null)
            {
                if (isBreathingEffectEnabled)
                {
                    var center = new Vector2((float)canvasWidth / 2, placement == SpectrumPlacement.Bottom ? (float)canvasHeight : 0);
                    ds.Transform = Matrix3x2.CreateScale(_breathingScale, center);
                }

                DrawGeometry(ds, _spectrumGeometry, fillColor, isGlowEffectEnabled, opacity, placement, canvasHeight);

                if (isBreathingEffectEnabled)
                {
                    ds.Transform = Matrix3x2.Identity;
                }
            }
        }

        public void Update(float bassEnergy, int breathingIntensity)
        {
            float maxScaleOffset = breathingIntensity / 100.0f;
            _targetBreathingScale = 1.0f + (bassEnergy * maxScaleOffset);

            if (_targetBreathingScale > _breathingScale)
            {
                // 鼓点出现，快速放大
                _breathingScale += (_targetBreathingScale - _breathingScale) * 0.2f;
            }
            else
            {
                // 鼓点消失，缓慢回落
                _breathingScale += (_targetBreathingScale - _breathingScale) * 0.05f;
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
            if (barCount < 2 || data == null || data.Length == 0) return null;

            float viewHeight = (float)height;

            float fixedScaleFactor = 0.05f * viewHeight;

            using var pathBuilder = new CanvasPathBuilder(creator);

            if (style == SpectrumStyle.Bar)
            {
                float totalStep = (float)width / barCount;
                float gap = 2.0f;
                // 防止条形太细导致消失
                float barWidth = totalStep - gap;
                if (barWidth < 1.0f)
                {
                    barWidth = totalStep;
                    gap = 0f;
                }
                // 如果 barWidth 很小，x 坐标微调
                float halfGap = gap / 2.0f;

                for (int i = 0; i < barCount; i++)
                {
                    float rawVal = i < data.Length ? data[i] : 0;

                    float barHeight = rawVal * fixedScaleFactor;

                    // 限制最大高度不超过画布，防止画出去
                    if (barHeight > viewHeight) barHeight = viewHeight;

                    // 忽略极小值，减少绘制开销
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

                    // Clamp
                    if (yVal > viewHeight) yVal = viewHeight;

                    // 处理 Y 轴翻转
                    float y = (placement == SpectrumPlacement.Bottom)
                        ? viewHeight - yVal
                        : yVal;

                    points[i] = new Vector2(i * pointSpacing, y);
                }

                // 绘制曲线
                pathBuilder.BeginFigure(points[0]);

                for (int i = 0; i < barCount - 1; i++)
                {
                    // Catmull-Rom 样条插值转贝塞尔控制点逻辑
                    // 边界检查优化
                    Vector2 p0 = points[i > 0 ? i - 1 : 0];
                    Vector2 p1 = points[i];
                    Vector2 p2 = points[i + 1];
                    Vector2 p3 = points[i + 2 < barCount ? i + 2 : barCount - 1];

                    // 简单的张力系数 (Tension)，0.16f (即 1/6) 是标准 Catmull-Rom
                    Vector2 cp1 = p1 + (p2 - p0) * 0.1666f;
                    Vector2 cp2 = p2 - (p3 - p1) * 0.1666f;

                    pathBuilder.AddCubicBezier(cp1, cp2, p2);
                }

                // 封口：连接底部/顶部直线以形成封闭区域用于填充
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

            return CanvasGeometry.CreatePath(pathBuilder);
        }

        private void DrawGeometry(
            CanvasDrawingSession ds,
            CanvasGeometry geometry,
            Color color,
            bool isGlowEffectEnabled,
            float opacity,
            SpectrumPlacement placement,
            double height)
        {
            var stops = new CanvasGradientStop[]
            {
                new() { Position = 0.0f, Color = Colors.Transparent },
                new() { Position = 1.0f, Color = Color.FromArgb((byte)(255 * opacity), color.R, color.G, color.B) }
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
                float glowOffsetY = placement == SpectrumPlacement.Bottom ? -4.0f : 4.0f;

                using (var layer = ds.CreateLayer(1.0f))
                {
                    // 让颜色叠加变亮
                    ds.Blend = CanvasBlend.Add;
                    ds.DrawImage(blurEffect, 0, glowOffsetY);
                    ds.Blend = CanvasBlend.SourceOver; // 还原混合模式
                }
            }

            ds.FillGeometry(geometry, brush);

            // (可选) 绘制一条高亮的描边，增强轮廓感，让波峰更清晰
            //ds.DrawGeometry(geometry, Colors.White, 1.0f);
        }

        public void Dispose()
        {
            _spectrumGeometry?.Dispose();
            _spectrumGeometry = null;
        }
    }
}