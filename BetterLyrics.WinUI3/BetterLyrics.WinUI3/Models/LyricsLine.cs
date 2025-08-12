// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Windows.Foundation;
using Windows.Graphics.Effects;
using Windows.UI;

namespace BetterLyrics.WinUI3.Models
{
    public class LyricsLine
    {
        private const double _animationDuration = 0.3;
        public ValueTransition<double> AngleTransition { get; set; } = new(
            initialValue: 0,
            durationSeconds: _animationDuration,
            easingType: EasingType.EaseInOutSine
        );
        public ValueTransition<double> BlurAmountTransition { get; set; } = new(
            initialValue: 0,
            durationSeconds: _animationDuration,
            easingType: EasingType.EaseInOutSine
        );
        public ValueTransition<double> HighlightOpacityTransition { get; set; } = new(
            initialValue: 0,
            durationSeconds: _animationDuration,
            easingType: EasingType.EaseInOutSine
        );
        public ValueTransition<double> OpacityTransition { get; set; } = new(
            initialValue: 0,
            durationSeconds: _animationDuration,
            easingType: EasingType.EaseInOutSine
        );
        public ValueTransition<double> ScaleTransition { get; set; } = new(
            initialValue: 0.75,
            durationSeconds: _animationDuration,
            easingType: EasingType.EaseInOutSine
        );
        public ValueTransition<double> YOffsetTransition { get; set; } = new(
            initialValue: 0,
            durationSeconds: 0.5,
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

        public void UpdateCenterPosition(double maxWidth, TextAlignmentType type)
        {
            if (CanvasTextLayout == null)
            {
                return;
            }
            double centerY = Position.Y + (double)CanvasTextLayout.LayoutBounds.Height;
            CenterPosition = type switch
            {
                TextAlignmentType.Left => new Vector2(Position.X, (float)centerY),
                TextAlignmentType.Center => new Vector2((float)(Position.X + maxWidth / 2.0), (float)centerY),
                TextAlignmentType.Right => new Vector2((float)(Position.X + maxWidth), (float)centerY),
                _ => throw new System.ArgumentOutOfRangeException(nameof(type), type, null),
            };
        }

        public void DisposeTextLayout()
        {
            CanvasTextLayout?.Dispose();
            CanvasTextLayout = null;
        }

        public void RecreateTextLayout(ICanvasAnimatedControl control, CanvasTextFormat textFormat, double maxWidth, double maxHeight, TextAlignmentType type)
        {
            DisposeTextLayout();
            CanvasTextLayout = new CanvasTextLayout(control, DisplayedText, textFormat, (float)maxWidth, (float)maxHeight);
            CanvasTextLayout.HorizontalAlignment = type.ToCanvasHorizontalAlignment();
        }

        public void DisposeTextGeometry()
        {
            TextGeometry?.Dispose();
            TextGeometry = null;
        }

        public void RecreateTextGeometry()
        {
            DisposeTextGeometry();
            if (CanvasTextLayout == null)
            {
                return;
            }
            TextGeometry = CanvasGeometry.CreateText(CanvasTextLayout);
        }
    }
}
