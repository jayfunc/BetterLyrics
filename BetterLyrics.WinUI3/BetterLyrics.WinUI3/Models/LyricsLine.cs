// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Collections.Generic;
using System.Numerics;
using Windows.UI;

namespace BetterLyrics.WinUI3.Models
{
    public class LyricsLine
    {
        private const float _animationDuration = 0.3f;
        public ValueTransition<float> AngleTransition { get; set; } = new(
            initialValue: 0f,
            durationSeconds: _animationDuration,
            easingType: EasingType.EaseInOutSine
        );
        public ValueTransition<float> BlurAmountTransition { get; set; } = new(
            initialValue: 0f,
            durationSeconds: _animationDuration,
            easingType: EasingType.EaseInOutSine
        );
        public ValueTransition<float> HighlightOpacityTransition { get; set; } = new(
            initialValue: 0f,
            durationSeconds: _animationDuration,
            easingType: EasingType.EaseInOutSine
        );
        public ValueTransition<float> OpacityTransition { get; set; } = new(
            initialValue: 0f,
            durationSeconds: _animationDuration,
            easingType: EasingType.EaseInOutSine
        );
        public ValueTransition<float> ScaleTransition { get; set; } = new(
            initialValue: 0f,
            durationSeconds: _animationDuration,
            easingType: EasingType.EaseInOutSine
        );

        public CanvasTextLayout? CanvasTextLayout { get; private set; }

        public Vector2 CenterPosition { get; private set; }
        public Vector2 Position { get; set; }

        public List<LyricsChar> LyricsChars { get; set; } = [];

        public int? DurationMs => EndMs - StartMs;
        public int? EndMs { get; set; }
        public int StartMs { get; set; }

        public string DisplayedText { get; set; } = "";
        public string OriginalText { get; set; } = "";

        public CanvasGeometry? TextGeometry { get; private set; }

        public CanvasCommandList? BackgroundFontEffect { get; private set; }
        public CanvasCommandList? ForegroundFontEffect { get; private set; }

        public void UpdateCenterPosition(float maxWidth, TextAlignmentType type)
        {
            if (CanvasTextLayout == null)
            {
                return;
            }
            float centerY = Position.Y + (float)CanvasTextLayout.LayoutBounds.Height;
            CenterPosition = type switch
            {
                TextAlignmentType.Left => new Vector2(Position.X, centerY),
                TextAlignmentType.Center => new Vector2(Position.X + maxWidth / 2, centerY),
                TextAlignmentType.Right => new Vector2(Position.X + maxWidth, centerY),
                _ => throw new System.ArgumentOutOfRangeException(nameof(type), type, null),
            };
        }

        public void UpdateTextLayout(ICanvasAnimatedControl control, CanvasTextFormat textFormat, float maxWidth, float maxHeight, TextAlignmentType type)
        {
            CanvasTextLayout?.Dispose();
            CanvasTextLayout = null;
            CanvasTextLayout = new CanvasTextLayout(control, DisplayedText, textFormat, maxWidth, maxHeight);
            CanvasTextLayout.HorizontalAlignment = type.ToCanvasHorizontalAlignment();
        }

        public void DisposeTextGeometry()
        {
            TextGeometry?.Dispose();
            TextGeometry = null;
        }

        public void UpdateTextGeometry()
        {
            DisposeTextGeometry();
            if (CanvasTextLayout == null)
            {
                return;
            }
            TextGeometry = CanvasGeometry.CreateText(CanvasTextLayout);
        }

        public void DisposeFontEffects()
        {
            BackgroundFontEffect?.Dispose();
            BackgroundFontEffect = null;
            ForegroundFontEffect?.Dispose();
            ForegroundFontEffect = null;
        }

        public void UpdateFontEffect(ICanvasAnimatedControl control, bool drawStroke, Color strokeColor, int strokeWidth, Color fontColor)
        {
            DisposeFontEffects();
            if (TextGeometry == null)
            {
                return;
            }
            BackgroundFontEffect = new CanvasCommandList(control);
            using var bgFontEffectDs = BackgroundFontEffect.CreateDrawingSession();
            ForegroundFontEffect = new CanvasCommandList(control);
            using var fgFontEffectDs = ForegroundFontEffect.CreateDrawingSession();
            if (drawStroke)
            {
                bgFontEffectDs.DrawGeometry(TextGeometry, Position, strokeColor, strokeWidth); // 描边
                fgFontEffectDs.DrawGeometry(TextGeometry, Position, strokeColor, strokeWidth); // 描边
            }
            bgFontEffectDs.FillGeometry(TextGeometry, Position, fontColor); // 填充
            fgFontEffectDs.FillGeometry(TextGeometry, Position, fontColor); // 填充
        }
    }
}
