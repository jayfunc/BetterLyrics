using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.UI;

namespace BetterLyrics.WinUI3.Renderer
{
    public class LyricsRenderer
    {
        private readonly PlayingLineRenderer _playingRenderer = new();
        private readonly UnplayingLineRenderer _unplayingRenderer = new();

        private Matrix4x4 _threeDimMatrix = Matrix4x4.Identity;

        public void Draw(
            ICanvasAnimatedControl control,
            CanvasDrawingSession ds,
            IList<RenderLyricsLine>? lines,
            int playingLineIndex,
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
            Color strokeColor,
            Color bgColor,
            Color fgColor,
            Func<int, LinePlaybackState> getPlaybackState)
        {
            using (var opacityLayer = ds.CreateLayer((float)lyricsOpacity))
            {
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
                                playingLineIndex,
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
                                strokeColor,
                                bgColor,
                                fgColor,
                                getPlaybackState);
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
                        playingLineIndex,
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
                        strokeColor,
                        bgColor,
                        fgColor,
                        getPlaybackState);
                }
            }
        }

        private void DrawLyrics(
            ICanvasAnimatedControl control,
            CanvasDrawingSession ds,
            IList<RenderLyricsLine>? lines,
            int playingLineIndex,
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
            Color strokeColor,
            Color bgColor,
            Color fgColor,
            Func<int, LinePlaybackState> getPlaybackState)
        {
            if (lines == null) return;

            var currentPlayingLine = lines.ElementAtOrDefault(playingLineIndex);
            if (currentPlayingLine == null) return;

            var effectSettings = windowStatus.LyricsEffectSettings;
            var styleSettings = windowStatus.LyricsStyleSettings;

            var rotationY = currentPlayingLine.OriginalPosition.WithX(effectSettings.FanLyricsAngle < 0 ? (float)lyricsWidth : 0);

            for (int i = startVisibleIndex; i <= endVisibleIndex; i++)
            {
                var line = lines.ElementAtOrDefault(i);
                if (line == null) continue;

                if (line.OriginalCanvasTextLayout == null) continue;
                if (line.OriginalCanvasTextLayout.LayoutBounds.Width <= 0) continue;

                double yOffset = line.YOffsetTransition.Value + userScrollOffset + lyricsY + lyricsHeight * playingLineTopOffsetFactor;

                var transform =
                    Matrix3x2.CreateScale((float)line.ScaleTransition.Value, line.CenterPosition) *
                    Matrix3x2.CreateRotation((float)line.AngleTransition.Value, rotationY) *
                    Matrix3x2.CreateTranslation((float)lyricsX, (float)yOffset);

                ds.Transform = transform;

                using (var textOnlyLayer = RenderBaseTextLayer(control, line, styleSettings.LyricsFontStrokeWidth, strokeColor, line.ColorTransition.Value))
                {
                    if (i == playingLineIndex)
                    {
                        var state = getPlaybackState(i);

                        _playingRenderer.Draw(control, ds, textOnlyLayer, line, state, bgColor, fgColor, effectSettings);
                    }
                    else
                    {
                        _unplayingRenderer.Draw(ds, textOnlyLayer, line);
                    }

                    if (i == mouseHoverLineIndex)
                    {
                        byte opacity = isMousePressing ? (byte)32 : (byte)16;
                        double scale = isMousePressing ? 1.09 : 1.10;
                        ds.FillRoundedRectangle(
                            new Windows.Foundation.Rect(line.TopLeftPosition.ToPoint().WithX(0), line.BottomRightPosition.ToPoint().WithX(lyricsWidth)).Scale(scale),
                            8, 8, Color.FromArgb(opacity, 255, 255, 255));
                    }
                }

                ds.Transform = Matrix3x2.Identity;
            }
        }

        private CanvasCommandList RenderBaseTextLayer(
            ICanvasResourceCreator resourceCreator,
            RenderLyricsLine line,
            double strokeWidth,
            Color strokeColor,
            Color fillColor)
        {
            var commandList = new CanvasCommandList(resourceCreator);
            using (var clds = commandList.CreateDrawingSession())
            {
                if (strokeWidth > 0)
                {
                    DrawGeometrySafely(clds, line.PhoneticCanvasGeometry, line.PhoneticPosition, strokeColor, strokeWidth);
                    DrawGeometrySafely(clds, line.OriginalCanvasGeometry, line.OriginalPosition, strokeColor, strokeWidth);
                    DrawGeometrySafely(clds, line.TranslatedCanvasGeometry, line.TranslatedPosition, strokeColor, strokeWidth);
                }

                DrawTextLayoutSafely(clds, line.PhoneticCanvasTextLayout, line.PhoneticPosition, fillColor);
                DrawTextLayoutSafely(clds, line.OriginalCanvasTextLayout, line.OriginalPosition, fillColor);
                DrawTextLayoutSafely(clds, line.TranslatedCanvasTextLayout, line.TranslatedPosition, fillColor);
            }
            return commandList;
        }

        private void DrawGeometrySafely(CanvasDrawingSession ds, CanvasGeometry? geo, Vector2 pos, Color color, double width)
        {
            if (geo == null) return;

            try
            {
                ds.DrawGeometry(geo, pos, color, (float)width);
            }
            catch (Exception) { }
        }

        private void DrawTextLayoutSafely(CanvasDrawingSession ds, CanvasTextLayout? layout, Vector2 pos, Color color)
        {
            if (layout == null) return;

            try
            {
                ds.DrawTextLayout(layout, pos, color);
            }
            catch (Exception) { }
        }

        public void CalculateLyrics3DMatrix(LyricsEffectSettings lyricsEffect, double lyricsX, double lyricsY, double lyricsWidth, double canvasHeight)
        {
            if (!lyricsEffect.Is3DLyricsEnabled) return;

            Vector3 center = new(
                (float)(lyricsX + lyricsWidth / 2),
                (float)(lyricsY + canvasHeight / 2),
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

    }
}
