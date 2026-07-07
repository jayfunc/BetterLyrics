using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Models.Lyrics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace BetterLyrics.WinUI3.Renderer.LyricsRenderer;

public partial class LyricsRenderer : EffectRendererBase, IDisposable
{
    private readonly EdgeFadeMaskRenderer _edgeFadeMaskRenderer = new();

    public int MouseHoverLineIndex { get; set; } = -1;
    public bool IsMousePressing { get; set; } = false;

    public int StartVisibleLineIndex { get; set; } = 0;
    public int EndVisibleLineIndex { get; set; } = 0;

    public double UserScrollOffset { get; set; } = 0;

    public double LyricsX { get; set; }
    public double LyricsY { get; set; }

    public double LyricsWidth { get; set; }
    public double LyricsHeight { get; set; }

    public double LyricsOpacity { get; set; } = 1;
    public double PlayingLineTopOffsetFactor { get; set; }

    public double CurrentProgressMs { get; set; }

    public LyricsWindowStatus? LyricsWindowStatus { get; set; }

    public IList<RenderLyricsLine>? RenderLyricsLines { get; set; }

    public void Dispose()
    {
        _edgeFadeMaskRenderer.Dispose();
    }

    public void Draw(ICanvasAnimatedControl control, CanvasDrawingSession ds)
    {
        if (LyricsOpacity == 0) return;

        if (LyricsWindowStatus == null) return;
        if (LyricsWindowStatus.LyricsEffectSettings.Is3DLyricsEnabled)
        {
            using var layer = new CanvasCommandList(control);
            using (var layerDs = layer.CreateDrawingSession())
            {
                DrawLyricsWithEdgeFadeHandled(control, layerDs);
            }

            DrawWithParallax(ds, layer);
        }
        else
        {
            DrawLyricsWithEdgeFadeHandled(control, ds);
        }
    }

    private void DrawLyricsWithEdgeFadeHandled(ICanvasAnimatedControl control, CanvasDrawingSession ds)
    {
        if (LyricsWindowStatus == null) return;
        if (_edgeFadeMaskRenderer.Brush != null &&
            (!LyricsWindowStatus.LyricsStyleSettings.AutoWrap ||
             LyricsWindowStatus.LyricsEffectSettings.IsLyricsEdgeFeatheringEffectEnabled))
            using (ds.CreateLayer(_edgeFadeMaskRenderer.Brush))
            {
                DrawLyrics(control, ds);
            }
        else
            DrawLyrics(control, ds);
    }

    private void DrawLyrics(ICanvasAnimatedControl control, CanvasDrawingSession ds)
    {
        if (RenderLyricsLines == null || LyricsWindowStatus == null) return;

        var effectSettings = LyricsWindowStatus.LyricsEffectSettings;
        var styleSettings = LyricsWindowStatus.LyricsStyleSettings;
        var isBreathingEnabled = effectSettings.IsLyricsBrethingEffectEnabled;

        var isVertical = styleSettings.LyricsLayoutOrientation == LyricsLayoutOrientation.Vertical;

        LyricsLineRendererBase lineRenderer = isVertical
            ? new VerticalLyricsLineRenderer()
            : new HorizontalLyricsLineRenderer();

        lineRenderer.StrokeWidth = styleSettings.LyricsFontStrokeWidth;
        lineRenderer.LyricsWidth = LyricsWidth;
        lineRenderer.LyricsHeight = LyricsHeight;
        lineRenderer.LyricsWindowStatus = LyricsWindowStatus;

        // 提前计算全局基准坐标
        var baseScrollX = isVertical
            ? UserScrollOffset + LyricsX + LyricsWidth * (1 - PlayingLineTopOffsetFactor)
            : LyricsX;

        var baseScrollY = isVertical
            ? LyricsY
            : UserScrollOffset + LyricsY + LyricsHeight * PlayingLineTopOffsetFactor;

        // 提前计算扇形歌词的旋转基准中心
        double fanAnchorX = 0;
        double fanAnchorY = 0;
        if (effectSettings.IsFanLyricsEnabled)
        {
            if (isVertical)
            {
                // 竖排：扇形围绕 Y 轴的顶部或底部展开
                fanAnchorY = effectSettings.FanLyricsAngle < 0 ? LyricsHeight : 0;
                fanAnchorY += LyricsHeight / 2 * (effectSettings.FanLyricsAngle < 0 ? 1 : -1);
            }
            else
            {
                // 横排：扇形围绕 X 轴的左侧或右侧展开
                fanAnchorX = effectSettings.FanLyricsAngle < 0 ? LyricsWidth : 0;
                fanAnchorX += LyricsWidth / 2 * (effectSettings.FanLyricsAngle < 0 ? 1 : -1);
            }
        }

        // 开始渲染循环
        for (var i = StartVisibleLineIndex; i <= EndVisibleLineIndex; i++)
        {
            if (i < 0 || i >= RenderLyricsLines.Count) continue;
            var line = RenderLyricsLines[i];

            if (line?.PrimaryTextLayout == null || line.PrimaryTextLayout.LayoutBounds.Width <= 0) continue;

            var isPlaying = line.GetIsPlaying(CurrentProgressMs);

            // 更新重用对象的动态属性
            lineRenderer.Line = line;
            lineRenderer.IsPlaying = isPlaying;
            lineRenderer.CurrentProgressMs = CurrentProgressMs;

            // 计算当前行的基础位移
            var currentXOffset = baseScrollX;
            var currentYOffset = baseScrollY;

            if (isVertical)
                currentXOffset += line.OffsetTransition.Value;
            else
                currentYOffset += line.OffsetTransition.Value;

            if (isPlaying) ApplyBreathingTransform(ds, line.CenterPosition, isBreathingEnabled);

            // 应用缩放矩阵
            ds.Transform *= Matrix3x2.CreateScale((float)line.ScaleTransition.Value, line.CenterPosition);

            // 应用扇形歌词矩阵
            if (effectSettings.IsFanLyricsEnabled)
            {
                var angle = (float)line.AngleTransition.Value;
                var angleRatio = Math.Abs(angle) / (Math.PI / 2);

                if (isVertical)
                {
                    // 竖排补偿 Y 轴位移，并绕 X 中心旋转
                    currentYOffset += angleRatio * (LyricsHeight / 2) *
                                      (effectSettings.FanLyricsAngle < 0 ? 1 : -1);
                    ds.Transform *= Matrix3x2.CreateRotation(angle,
                        new Vector2(line.CenterPosition.X, (float)fanAnchorY));
                }
                else
                {
                    // 横排补偿 X 轴位移，并绕 Y 中心旋转
                    currentXOffset += angleRatio * (LyricsWidth / 2) * (effectSettings.FanLyricsAngle < 0 ? 1 : -1);
                    ds.Transform *= Matrix3x2.CreateRotation(angle,
                        new Vector2((float)fanAnchorX, line.CenterPosition.Y));
                }
            }

            // 应用最终位移矩阵
            ds.Transform *= Matrix3x2.CreateTranslation((float)currentXOffset, (float)currentYOffset);

            line.EnsureCaches(control, styleSettings.LyricsFontStrokeWidth);
            if (line.CachedStroke == null || line.CachedFill == null) continue;
            if (line.UnplayedFillTint == null || line.UnplayedStrokeTint == null ||
                line.UnplayedComposite == null) continue;

            line.UnplayedFillTint.Color = ColorExtensions.FromAppColor(line.UnplayedFillColorTransition.Value);
            line.UnplayedStrokeTint.Color = ColorExtensions.FromAppColor(line.UnplayedStrokeColorTransition.Value);

            // 真正的绘制调用
            lineRenderer.Draw(control, ds);

            // 鼠标悬停的高亮背景框
            if (i == MouseHoverLineIndex)
            {
                var opacity = IsMousePressing ? (byte)32 : (byte)16;
                var scale = IsMousePressing ? 1.09 : 1.10;
                // 这里的 TopLeft 和 BottomRight 无论横竖排都是绝对包围盒，所以可以直接 Scale
                ds.FillRoundedRectangle(
                    new Rect(line.TopLeftPosition.ToPoint(), line.BottomRightPosition.ToPoint()).Scale(scale),
                    8, 8, Color.FromArgb(opacity, 255, 255, 255));
            }

#if DEBUG && false
                ds.DrawRectangle(new Rect(line.TopLeftPosition.ToPoint(), line.BottomRightPosition.ToPoint()), Colors.Cyan);

                // Debug 线也需要区分横竖排，如果是竖排，辅助线应该是垂直的
                if (isVertical)
                    ds.DrawLine(new Vector2(line.CenterPosition.X, line.TopLeftPosition.Y), new Vector2(line.CenterPosition.X, line.BottomRightPosition.Y), Colors.Cyan);
                else
                    ds.DrawLine(new Vector2(line.TopLeftPosition.X, line.CenterPosition.Y), new Vector2(line.BottomRightPosition.X, line.CenterPosition.Y), Colors.Cyan);

                ds.DrawText($"({line.CenterPosition.X}, {line.CenterPosition.Y})", line.CenterPosition, Colors.Cyan);
#endif

            ds.Transform = Matrix3x2.Identity;
        }
    }

    private void UpdateLyricsParallaxMatrix()
    {
        if (LyricsWindowStatus == null) return;
        var lyricsStyle = LyricsWindowStatus.LyricsStyleSettings;
        var lyricsEffect = LyricsWindowStatus.LyricsEffectSettings;

        if (!lyricsEffect.Is3DLyricsEnabled)
        {
            _threeDimMatrix = Matrix4x4.Identity;
            return;
        }

        var playingLineTopOffsetFactor = lyricsStyle.PlayingLineTopOffset / 100.0;

        // 获取旋转中心点
        Vector3 center = new(
            (float)(LyricsX + LyricsWidth / 2),
            (float)(LyricsY + LyricsHeight * playingLineTopOffsetFactor),
            0);

        UpdateParallaxMatrix(
            center,
            lyricsEffect.IsAuto3DLyricsEnabled,
            lyricsEffect.Lyrics3DXAngle,
            lyricsEffect.Lyrics3DYAngle,
            lyricsEffect.Lyrics3DZAngle,
            lyricsEffect.IsAuto3DLyricsEnabled ? 800f : lyricsEffect.Lyrics3DDepth
        );
    }

    private void UpdateEdgeFadeMask(ICanvasAnimatedControl sender)
    {
        if (LyricsWindowStatus == null) return;

        var isVertical = LyricsWindowStatus.LyricsStyleSettings.LyricsLayoutOrientation ==
                         LyricsLayoutOrientation.Vertical;

        var autoWrapFadeWidth = LyricsWindowStatus.LyricsStyleSettings.AutoWrap ? 0 : 16;
        var lyricsEdgeFadeWidth =
            LyricsWindowStatus.LyricsEffectSettings.IsLyricsEdgeFeatheringEffectEnabled ? 16 : 0;

        if (isVertical)
            _edgeFadeMaskRenderer.Update(sender, new Rect(LyricsX, LyricsY - 16, LyricsWidth, LyricsHeight + 32),
                lyricsEdgeFadeWidth, autoWrapFadeWidth, lyricsEdgeFadeWidth, autoWrapFadeWidth);
        else
            _edgeFadeMaskRenderer.Update(sender, new Rect(LyricsX - 16, LyricsY, LyricsWidth + 32, LyricsHeight),
                autoWrapFadeWidth, lyricsEdgeFadeWidth, autoWrapFadeWidth, lyricsEdgeFadeWidth);
    }

    public void Update(ICanvasAnimatedControl sender, float bassEnergy, int breathingIntensity)
    {
        UpdateBreathing(bassEnergy, breathingIntensity);
        UpdateLyricsParallaxMatrix();
        UpdateEdgeFadeMask(sender);
    }
}