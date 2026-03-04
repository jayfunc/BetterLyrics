using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Models.Settings;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.UI;

namespace BetterLyrics.WinUI3.Renderer
{
    public class LyricsRenderer : BreathingRendererBase
    {
        private Matrix4x4 _threeDimMatrix = Matrix4x4.Identity;

        public void Draw(
            ICanvasAnimatedControl control,
            CanvasDrawingSession ds,
            IList<RenderLyricsLine>? lines,
            int mouseHoverLineIndex,
            bool isMousePressing,
            int startVisibleIndex,
            int endVisibleIndex,
            double lyricsX,
            double lyricsY,
            double lyricsWidth,
            double lyricsHeight,
            double userScrollOffset,
            double lyricsOpacity,
            double playingLineTopOffsetFactor,
            LyricsWindowStatus windowStatus,
            double currentProgressMs)
        {
            if (lyricsOpacity == 0) return;

            if (windowStatus.LyricsEffectSettings.Is3DLyricsEnabled)
            {
                using (var layer = new CanvasCommandList(control))
                {
                    using (var layerDs = layer.CreateDrawingSession())
                    {
                        DrawLyrics(
                            control,
                            layerDs,
                            lines,
                            mouseHoverLineIndex,
                            isMousePressing,
                            startVisibleIndex,
                            endVisibleIndex,
                            lyricsX,
                            lyricsY,
                            lyricsWidth,
                            lyricsHeight,
                            userScrollOffset,
                            playingLineTopOffsetFactor,
                            windowStatus,
                            currentProgressMs);
                    }

                    ds.DrawImage(new Transform3DEffect
                    {
                        Source = layer,
                        TransformMatrix = _threeDimMatrix
                    });
                }
            }
            else
            {
                DrawLyrics(
                    control,
                    ds,
                    lines,
                    mouseHoverLineIndex,
                    isMousePressing,
                    startVisibleIndex,
                    endVisibleIndex,
                    lyricsX,
                    lyricsY,
                    lyricsWidth,
                    lyricsHeight,
                    userScrollOffset,
                    playingLineTopOffsetFactor,
                    windowStatus,
                    currentProgressMs);
            }
        }

        private void DrawLyrics(
            ICanvasAnimatedControl control,
            CanvasDrawingSession ds,
            IList<RenderLyricsLine>? lines,
            int mouseHoverLineIndex,
            bool isMousePressing,
            int startVisibleIndex,
            int endVisibleIndex,
            double lyricsX,
            double lyricsY,
            double lyricsWidth,
            double lyricsHeight,
            double userScrollOffset,
            double playingLineTopOffsetFactor,
            LyricsWindowStatus windowStatus,
            double currentProgressMs)
        {
            if (lines == null) return;

            var effectSettings = windowStatus.LyricsEffectSettings;
            var styleSettings = windowStatus.LyricsStyleSettings;
            var isBreathingEnabled = windowStatus.LyricsEffectSettings.IsLyricsBrethingEffectEnabled;

            var rotationX = effectSettings.FanLyricsAngle < 0 ? lyricsWidth : 0;
            rotationX += lyricsWidth / 2 * (effectSettings.FanLyricsAngle < 0 ? 1 : -1);

            var yOffsetBase = userScrollOffset + lyricsY + lyricsHeight * playingLineTopOffsetFactor;

            for (int i = startVisibleIndex; i <= endVisibleIndex; i++)
            {
                if (i < 0 || i >= lines.Count) continue;
                var line = lines[i];

                if (line == null) continue;
                if (line.PrimaryTextLayout == null) continue;
                if (line.PrimaryTextLayout.LayoutBounds.Width <= 0) continue;

                double xOffset = lyricsX;
                double yOffset = line.YOffsetTransition.Value + yOffsetBase;

                bool isPlaying = line.GetIsPlaying(currentProgressMs);

                if (isPlaying)
                {
                    ApplyBreathingTransform(ds, line.CenterPosition, isBreathingEnabled);
                }

                ds.Transform *= Matrix3x2.CreateScale((float)line.ScaleTransition.Value, line.CenterPosition);

                if (effectSettings.IsFanLyricsEnabled)
                {
                    xOffset += Math.Abs(line.AngleTransition.Value) / (Math.PI / 2) * lyricsWidth / 2 * (effectSettings.FanLyricsAngle < 0 ? 1 : -1);
                    var rotationY = line.CenterPosition.Y;
                    ds.Transform *= Matrix3x2.CreateRotation((float)line.AngleTransition.Value, new Vector2((float)rotationX, rotationY));
                }

                ds.Transform *= Matrix3x2.CreateTranslation((float)xOffset, (float)yOffset);

                line.EnsureCaches(control, styleSettings.LyricsFontStrokeWidth);
                if (line.CachedStroke == null || line.CachedFill == null) continue;
                if (line.UnplayedFillTint == null || line.UnplayedStrokeTint == null || line.UnplayedComposite == null) continue;

                line.UnplayedFillTint.Color = line.UnplayedFillColorTransition.Value;
                line.UnplayedStrokeTint.Color = line.UnplayedStrokeColorTransition.Value;

                if (isPlaying)
                {
                    PlayingLineRenderer.Draw(control, ds, styleSettings.LyricsFontStrokeWidth, line.CachedStroke, line.CachedFill, line.UnplayedComposite, line, currentProgressMs, effectSettings);
                }
                else
                {
                    UnplayingLineRenderer.Draw(ds, line.UnplayedComposite, styleSettings.LyricsFontStrokeWidth, line);
                }

                if (i == mouseHoverLineIndex)
                {
                    byte opacity = isMousePressing ? (byte)32 : (byte)16;
                    double scale = isMousePressing ? 1.09 : 1.10;
                    ds.FillRoundedRectangle(
                        new Windows.Foundation.Rect(line.TopLeftPosition.ToPoint().WithX(0), line.BottomRightPosition.ToPoint().WithX(lyricsWidth)).Scale(scale),
                        8, 8, Color.FromArgb(opacity, 255, 255, 255));
                }

                ds.Transform = Matrix3x2.Identity;
            }
        }

        public void CalculateLyrics3DMatrix(
            LyricsStyleSettings lyricsStyle, LyricsEffectSettings lyricsEffect,
            double lyricsX, double lyricsY,
            double lyricsWidth, double lyricsHeight,
            bool isLayoutChanged
        )
        {
            if (!isLayoutChanged) return;
            if (!lyricsEffect.Is3DLyricsEnabled) return;

            var playingLineTopOffsetFactor = lyricsStyle.PlayingLineTopOffset / 100.0;

            Vector3 center = new(
                (float)(lyricsX + lyricsWidth / 2),
                (float)(lyricsY + lyricsHeight * playingLineTopOffsetFactor),
                0);

            float rotationX = (float)(Math.PI * lyricsEffect.Lyrics3DXAngle / 180.0);
            float rotationY = (float)(Math.PI * lyricsEffect.Lyrics3DYAngle / 180.0);
            float rotationZ = (float)(Math.PI * lyricsEffect.Lyrics3DZAngle / 180.0);

            Matrix4x4 rotation =
                Matrix4x4.CreateRotationX(rotationX) *
                Matrix4x4.CreateRotationY(rotationY) *
                Matrix4x4.CreateRotationZ(rotationZ);
            Matrix4x4 perspective = Matrix4x4.Identity;
            perspective.M34 = 1.0f / lyricsEffect.Lyrics3DDepth;

            // 组合变换：
            // 1. 将中心移到原点
            // 2. 旋转
            // 3. 应用透视
            // 4. 将中心移回原位
            _threeDimMatrix =
                Matrix4x4.CreateTranslation(-center) *
                rotation *
                perspective *
                Matrix4x4.CreateTranslation(center);
        }

        public void Update(float bassEnergy, int breathingIntensity)
        {
            base.UpdateBreathing(bassEnergy, breathingIntensity);
        }

    }
}
