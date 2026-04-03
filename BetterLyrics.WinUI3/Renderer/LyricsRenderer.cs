using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Models.Settings;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.UI;

namespace BetterLyrics.WinUI3.Renderer
{
    public partial class LyricsRenderer : BreathingRendererBase, IDisposable
    {
        private Matrix4x4 _threeDimMatrix = Matrix4x4.Identity;
        private EdgeFadeMaskRenderer _edgeFadeMaskRenderer = new();

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

                ds.DrawImage(new Transform3DEffect
                {
                    Source = layer,
                    TransformMatrix = _threeDimMatrix
                });
            }
            else
            {
                DrawLyricsWithEdgeFadeHandled(control, ds);
            }
        }

        private void DrawLyricsWithEdgeFadeHandled(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (LyricsWindowStatus == null) return;
            if (_edgeFadeMaskRenderer.Brush != null && !LyricsWindowStatus.LyricsStyleSettings.AutoWrap)
            {
                using (ds.CreateLayer(_edgeFadeMaskRenderer.Brush))
                {
                    DrawLyrics(control, ds);
                }
            }
            else
            {
                DrawLyrics(control, ds);
            }
        }

        private void DrawLyrics(ICanvasAnimatedControl control, CanvasDrawingSession ds)
        {
            if (RenderLyricsLines == null) return;
            if (LyricsWindowStatus == null) return;

            var effectSettings = LyricsWindowStatus.LyricsEffectSettings;
            var styleSettings = LyricsWindowStatus.LyricsStyleSettings;
            var isBreathingEnabled = LyricsWindowStatus.LyricsEffectSettings.IsLyricsBrethingEffectEnabled;

            var rotationX = effectSettings.FanLyricsAngle < 0 ? LyricsWidth : 0;
            rotationX += LyricsWidth / 2 * (effectSettings.FanLyricsAngle < 0 ? 1 : -1);

            var yOffsetBase = UserScrollOffset + LyricsY + LyricsHeight * PlayingLineTopOffsetFactor;

            for (int i = StartVisibleLineIndex; i <= EndVisibleLineIndex; i++)
            {
                if (i < 0 || i >= RenderLyricsLines.Count) continue;
                var line = RenderLyricsLines[i];

                if (line == null) continue;
                if (line.PrimaryTextLayout == null) continue;
                if (line.PrimaryTextLayout.LayoutBounds.Width <= 0) continue;

                double xOffset = LyricsX;
                double yOffset = line.YOffsetTransition.Value + yOffsetBase;

                bool isPlaying = line.GetIsPlaying(CurrentProgressMs);

                LyricsLineRenderer lineRenderer = new()
                {
                    IsPlaying = isPlaying,
                    StrokeWidth = styleSettings.LyricsFontStrokeWidth,
                    CurrentProgressMs = CurrentProgressMs,
                    LyricsWidth = LyricsWidth,
                    LyricsHeight = LyricsHeight,
                    Line = line,
                    LyricsWindowStatus = LyricsWindowStatus
                };

                if (isPlaying)
                {
                    ApplyBreathingTransform(ds, line.CenterPosition, isBreathingEnabled);
                }

                ds.Transform *= Matrix3x2.CreateScale((float)line.ScaleTransition.Value, line.CenterPosition);

                if (effectSettings.IsFanLyricsEnabled)
                {
                    xOffset += Math.Abs(line.AngleTransition.Value) / (Math.PI / 2) * LyricsWidth / 2 * (effectSettings.FanLyricsAngle < 0 ? 1 : -1);
                    var rotationY = line.CenterPosition.Y;
                    ds.Transform *= Matrix3x2.CreateRotation((float)line.AngleTransition.Value, new Vector2((float)rotationX, rotationY));
                }

                ds.Transform *= Matrix3x2.CreateTranslation((float)xOffset, (float)yOffset);

                line.EnsureCaches(control, styleSettings.LyricsFontStrokeWidth);
                if (line.CachedStroke == null || line.CachedFill == null) continue;
                if (line.UnplayedFillTint == null || line.UnplayedStrokeTint == null || line.UnplayedComposite == null) continue;

                line.UnplayedFillTint.Color = line.UnplayedFillColorTransition.Value;
                line.UnplayedStrokeTint.Color = line.UnplayedStrokeColorTransition.Value;

                lineRenderer.Draw(control, ds);

                if (i == MouseHoverLineIndex)
                {
                    byte opacity = IsMousePressing ? (byte)32 : (byte)16;
                    double scale = IsMousePressing ? 1.09 : 1.10;
                    ds.FillRoundedRectangle(
                        new Windows.Foundation.Rect(line.TopLeftPosition.ToPoint(), line.BottomRightPosition.ToPoint()).Scale(scale),
                        8, 8, Color.FromArgb(opacity, 255, 255, 255));
                }

#if DEBUG
                //ds.DrawRectangle(new Windows.Foundation.Rect(line.TopLeftPosition.ToPoint(), line.BottomRightPosition.ToPoint()), Colors.Cyan);
                //ds.DrawLine(new Vector2(line.TopLeftPosition.X, line.CenterPosition.Y), new Vector2(line.BottomRightPosition.X, line.CenterPosition.Y), Colors.Cyan);
                //ds.DrawText($"({line.CenterPosition.X}, {line.CenterPosition.Y})", line.CenterPosition, Colors.Cyan);
#endif

                ds.Transform = Matrix3x2.Identity;
            }
        }

        public void CalculateLyrics3DMatrix(bool isLayoutChanged)
        {
            if (!isLayoutChanged) return;

            if (LyricsWindowStatus == null) return;
            var lyricsStyle = LyricsWindowStatus.LyricsStyleSettings;
            var lyricsEffect = LyricsWindowStatus.LyricsEffectSettings;

            if (!lyricsEffect.Is3DLyricsEnabled) return;

            var playingLineTopOffsetFactor = lyricsStyle.PlayingLineTopOffset / 100.0;

            Vector3 center = new(
                (float)(LyricsX + LyricsWidth / 2),
                (float)(LyricsY + LyricsHeight * playingLineTopOffsetFactor),
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

        public void Update(ICanvasAnimatedControl sender, float bassEnergy, int breathingIntensity)
        {
            base.UpdateBreathing(bassEnergy, breathingIntensity);
            switch (LyricsWindowStatus?.LyricsStyleSettings.LyricsLineContentOrientation)
            {
                case Enums.LyricsLineContentOrientation.Horizontal:
                    var stops = new CanvasGradientStop[]
                    {
                        new() { Position = 0.00f, Color = Colors.Transparent },

                        new() { Position = 0.05f, Color = Colors.White },
                        new() { Position = 0.45f, Color = Colors.White },
                        
                        new() { Position = 0.50f, Color = Colors.Transparent },

                        new() { Position = 0.55f, Color = Colors.White },
                        new() { Position = 0.95f, Color = Colors.White },

                        new() { Position = 1.00f, Color = Colors.Transparent }
                    };
                    _edgeFadeMaskRenderer.Update(
                        sender,
                        new Windows.Foundation.Rect(LyricsX - 16, LyricsY, LyricsWidth + 32, LyricsHeight),
                        stops,
                        false
                    );
                    break;
                case Enums.LyricsLineContentOrientation.Vertical:
                    _edgeFadeMaskRenderer.Update(
                        sender,
                        new Windows.Foundation.Rect(LyricsX - 16, LyricsY, LyricsWidth + 32, LyricsHeight),
                        16, 0, 16, 0
                    );
                    break;
                default:
                    break;
            }
        }

        public void Dispose()
        {
            _edgeFadeMaskRenderer.Dispose();
        }

    }
}
