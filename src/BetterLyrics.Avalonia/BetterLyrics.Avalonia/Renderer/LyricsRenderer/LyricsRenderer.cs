using System;
using Avalonia;
using Avalonia.Media;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Avalonia.Extensions; // Adjusted namespace
using BetterLyrics.Avalonia.Models.Lyrics;
using System.Collections.Generic; // Adjusted namespace

namespace BetterLyrics.Avalonia.Renderer.LyricsRenderer;

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

    public void Draw(DrawingContext context)
    {
        if (LyricsOpacity <= 0 || LyricsWindowStatus == null) return;

        // Apply global opacity
        using (context.PushOpacity(LyricsOpacity))
        {
            if (LyricsWindowStatus.LyricsEffectSettings.Is3DLyricsEnabled && ParallaxContext != null)
            {
                // In pure Avalonia DrawingContext, we approximate the 3D depth parallax using a 2D translation matrix.
                // This keeps text rendering blisteringly fast (CPU vectoring) compared to forcing a RenderTargetBitmap buffer.
                var pTransform = Matrix.CreateTranslation(
                    ParallaxContext.CurrentTranslateX, 
                    ParallaxContext.CurrentTranslateY);

                using (context.PushTransform(pTransform))
                {
                    DrawLyricsWithEdgeFadeHandled(context);
                }
            }
            else
            {
                DrawLyricsWithEdgeFadeHandled(context);
            }
        }
    }

    private void DrawLyricsWithEdgeFadeHandled(DrawingContext context)
    {
        if (LyricsWindowStatus == null) return;
        
        if (_edgeFadeMaskRenderer.Brush != null &&
            (!LyricsWindowStatus.LyricsStyleSettings.AutoWrap ||
             LyricsWindowStatus.LyricsEffectSettings.IsLyricsEdgeFeatheringEffectEnabled))
        {
            var isVertical = LyricsWindowStatus.LyricsStyleSettings.LyricsLayoutOrientation == LyricsLayoutOrientation.Vertical;
            var maskBounds = isVertical
                ? new Rect(LyricsX, LyricsY - 16, LyricsWidth, LyricsHeight + 32)
                : new Rect(LyricsX - 16, LyricsY, LyricsWidth + 32, LyricsHeight);

            // Replaces ds.CreateLayer(_edgeFadeMaskRenderer.Brush)
            using (context.PushOpacityMask(_edgeFadeMaskRenderer.Brush, maskBounds))
            {
                DrawLyrics(context);
            }
        }
        else
        {
            DrawLyrics(context);
        }
    }

    private void DrawLyrics(DrawingContext context)
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

        var baseScrollX = isVertical
            ? UserScrollOffset + LyricsX + LyricsWidth * (1 - PlayingLineTopOffsetFactor)
            : LyricsX;

        var baseScrollY = isVertical
            ? LyricsY
            : UserScrollOffset + LyricsY + LyricsHeight * PlayingLineTopOffsetFactor;

        double fanAnchorX = 0;
        double fanAnchorY = 0;
        
        if (effectSettings.IsFanLyricsEnabled)
        {
            if (isVertical)
            {
                fanAnchorY = effectSettings.FanLyricsAngle < 0 ? LyricsHeight : 0;
                fanAnchorY += LyricsHeight / 2 * (effectSettings.FanLyricsAngle < 0 ? 1 : -1);
            }
            else
            {
                fanAnchorX = effectSettings.FanLyricsAngle < 0 ? LyricsWidth : 0;
                fanAnchorX += LyricsWidth / 2 * (effectSettings.FanLyricsAngle < 0 ? 1 : -1);
            }
        }

        for (var i = StartVisibleLineIndex; i <= EndVisibleLineIndex; i++)
        {
            if (i < 0 || i >= RenderLyricsLines.Count) continue;
            var line = RenderLyricsLines[i];

            if (line?.PrimaryTextLayout == null || line.PrimaryTextLayout.Width <= 0) continue;

            var isPlaying = line.GetIsPlaying(CurrentProgressMs);

            lineRenderer.Line = line;
            lineRenderer.IsPlaying = isPlaying;
            lineRenderer.CurrentProgressMs = CurrentProgressMs;

            var currentXOffset = baseScrollX;
            var currentYOffset = baseScrollY;

            if (isVertical)
                currentXOffset += line.OffsetTransition.Value;
            else
                currentYOffset += line.OffsetTransition.Value;

            // Compute cumulative matrix
            var transform = Matrix.Identity;

            // 1. Scale Matrix (Breathing and Standard Scale)
            var scale = line.ScaleTransition.Value;
            if (isPlaying && isBreathingEnabled)
            {
                // Multiply the scale transition with the bass energy scale inherited from EffectRendererBase
                scale *= _currentScale; 
            }
            transform *= Matrix.CreateTranslation(-line.CenterPosition.X, -line.CenterPosition.Y) * 
                         Matrix.CreateScale(scale, scale) * 
                         Matrix.CreateTranslation(line.CenterPosition.X, line.CenterPosition.Y);

            // 2. Rotation Matrix (Fan Lyrics)
            if (effectSettings.IsFanLyricsEnabled)
            {
                var angle = line.AngleTransition.Value;
                var angleRatio = Math.Abs(angle) / (Math.PI / 2);

                if (isVertical)
                {
                    currentYOffset += angleRatio * (LyricsHeight / 2) * (effectSettings.FanLyricsAngle < 0 ? 1 : -1);
                    transform *= Matrix.CreateRotation(angle, new Point(line.CenterPosition.X, fanAnchorY));
                }
                else
                {
                    currentXOffset += angleRatio * (LyricsWidth / 2) * (effectSettings.FanLyricsAngle < 0 ? 1 : -1);
                    transform *= Matrix.CreateRotation(angle, new Point(fanAnchorX, line.CenterPosition.Y));
                }
            }

            // 3. Translation Matrix
            transform *= Matrix.CreateTranslation(currentXOffset, currentYOffset);

            // Ensure visual cache logic built in step 6
            line.EnsureCaches(styleSettings.LyricsFontStrokeWidth);
            if (line.CachedStroke == null && line.CachedFill == null) continue;

            // Note: Tint color passing is removed because lineRenderer inherently reads 
            // the transition values directly in the new Avalonia base class implementation.

            // Apply transform and draw
            using (context.PushTransform(transform))
            {
                lineRenderer.Draw(context);

                // Mouse hover highlight
                if (i == MouseHoverLineIndex)
                {
                    var opacity = IsMousePressing ? (byte)32 : (byte)16;
                    var hoverScale = IsMousePressing ? 1.09 : 1.10;
                    
                    var rect = new Rect(
                        line.TopLeftPosition.X, 
                        line.TopLeftPosition.Y, 
                        line.BottomRightPosition.X - line.TopLeftPosition.X, 
                        line.BottomRightPosition.Y - line.TopLeftPosition.Y);

                    // Scale the rect out from its center
                    var center = rect.Center;
                    rect = new Rect(
                        center.X + (rect.X - center.X) * hoverScale, 
                        center.Y + (rect.Y - center.Y) * hoverScale, 
                        rect.Width * hoverScale, 
                        rect.Height * hoverScale);

                    var brush = new SolidColorBrush(Color.FromArgb(opacity, 255, 255, 255));
                    context.DrawRectangle(brush, null, rect, 8, 8); // 8px corner radius
                }
            }
        }
    }

    private void UpdateLyricsParallaxMatrix()
    {
        if (LyricsWindowStatus == null) return;
        var lyricsStyle = LyricsWindowStatus.LyricsStyleSettings;
        var lyricsEffect = LyricsWindowStatus.LyricsEffectSettings;

        if (!lyricsEffect.Is3DLyricsEnabled)
        {
            _threeDimMatrix = System.Numerics.Matrix4x4.Identity;
            return;
        }

        var playingLineTopOffsetFactor = lyricsStyle.PlayingLineTopOffset / 100.0;

        System.Numerics.Vector3 center = new(
            (float)(LyricsX + LyricsWidth / 2),
            (float)(LyricsY + LyricsHeight * playingLineTopOffsetFactor),
            0);

        // Retain 3D calculation logic from EffectRendererBase to power the fallback 2D vectors
        UpdateParallaxMatrix(
            center,
            lyricsEffect.IsAuto3DLyricsEnabled,
            lyricsEffect.Lyrics3DXAngle,
            lyricsEffect.Lyrics3DYAngle,
            lyricsEffect.Lyrics3DZAngle,
            lyricsEffect.IsAuto3DLyricsEnabled ? 800f : lyricsEffect.Lyrics3DDepth
        );
    }

    private void UpdateEdgeFadeMask()
    {
        if (LyricsWindowStatus == null) return;

        var isVertical = LyricsWindowStatus.LyricsStyleSettings.LyricsLayoutOrientation == LyricsLayoutOrientation.Vertical;
        var autoWrapFadeWidth = LyricsWindowStatus.LyricsStyleSettings.AutoWrap ? 0 : 16;
        var lyricsEdgeFadeWidth = LyricsWindowStatus.LyricsEffectSettings.IsLyricsEdgeFeatheringEffectEnabled ? 16 : 0;

        // Note: EdgeFadeMaskRenderer parameters updated to match Avalonia Size/Rect signatures without ICanvasAnimatedControl
        if (isVertical)
        {
            _edgeFadeMaskRenderer.Update(
                new Rect(LyricsX, LyricsY - 16, LyricsWidth, LyricsHeight + 32),
                lyricsEdgeFadeWidth, autoWrapFadeWidth, lyricsEdgeFadeWidth, autoWrapFadeWidth);
        }
        else
        {
            _edgeFadeMaskRenderer.Update(
                new Rect(LyricsX - 16, LyricsY, LyricsWidth + 32, LyricsHeight),
                autoWrapFadeWidth, lyricsEdgeFadeWidth, autoWrapFadeWidth, lyricsEdgeFadeWidth);
        }
    }

    // signature stripped of ICanvasAnimatedControl sender
    public void Update(float bassEnergy, int breathingIntensity)
    {
        UpdateBreathing(bassEnergy, breathingIntensity);
        UpdateLyricsParallaxMatrix();
        UpdateEdgeFadeMask();
    }
}