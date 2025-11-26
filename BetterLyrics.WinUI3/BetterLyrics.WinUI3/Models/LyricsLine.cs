// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Collections.Generic;
using System.Numerics;

namespace BetterLyrics.WinUI3.Models
{
    public class LyricsLine
    {
        public double AnimationDuration { get; set; } = 0.3;
        public ValueTransition<double> AngleTransition { get; set; }
        public ValueTransition<double> BlurAmountTransition { get; set; }
        public ValueTransition<double> PhoneticTextOpacityTransition { get; set; }
        public ValueTransition<double> OriginalTextOpacityTransition { get; set; }
        public ValueTransition<double> TranslatedTextOpacityTransition { get; set; }
        public ValueTransition<double> ScaleTransition { get; set; }
        public ValueTransition<double> YOffsetTransition { get; set; }

        public CanvasTextLayout? OriginalCanvasTextLayout { get; private set; }
        public CanvasTextLayout? TranslatedCanvasTextLayout { get; private set; }
        public CanvasTextLayout? PhoneticCanvasTextLayout { get; private set; }

        public Vector2 CenterPosition { get; private set; }
        /// <summary>
        /// 原文位置
        /// </summary>
        public Vector2 OriginalPosition { get; set; }
        /// <summary>
        /// 译文位置
        /// </summary>
        public Vector2 TranslatedPosition { get; set; }
        /// <summary>
        /// 注音位置
        /// </summary>
        public Vector2 PhoneticPosition { get; set; }

        public List<LyricsSyllable> LyricsSyllables { get; set; } = [];

        public int? DurationMs => EndMs - StartMs;
        public int? EndMs { get; set; }
        public int StartMs { get; set; }

        /// <summary>
        /// 原文
        /// </summary>
        public string OriginalText { get; set; } = "";
        /// <summary>
        /// 译文
        /// </summary>
        public string TranslatedText { get; set; } = "";
        /// <summary>
        /// 注音
        /// </summary>
        public string PhoneticText { get; set; } = "";

        public CanvasGeometry? OriginalCanvasGeometry { get; private set; }
        public CanvasGeometry? TranslatedCanvasGeometry { get; private set; }
        public CanvasGeometry? PhoneticCanvasGeometry { get; private set; }

        public LyricsLine()
        {
            AngleTransition = new(
                initialValue: 0,
                durationSeconds: AnimationDuration,
                easingType: EasingType.EaseInOutQuad
            );
            BlurAmountTransition = new(
                 initialValue: 0,
                 durationSeconds: AnimationDuration,
                 easingType: EasingType.EaseInOutQuad
             );
            PhoneticTextOpacityTransition = new(
                initialValue: 0,
                durationSeconds: AnimationDuration,
                easingType: EasingType.EaseInOutQuad
            );
            OriginalTextOpacityTransition = new(
                initialValue: 0,
                durationSeconds: AnimationDuration,
                easingType: EasingType.EaseInOutQuad
            );
            TranslatedTextOpacityTransition = new(
                initialValue: 0,
                durationSeconds: AnimationDuration,
                easingType: EasingType.EaseInOutQuad
            );
            ScaleTransition = new(
                initialValue: 0,
                durationSeconds: AnimationDuration,
                easingType: EasingType.EaseInOutQuad
            );
            YOffsetTransition = new(
                initialValue: 0,
                durationSeconds: AnimationDuration,
                easingType: EasingType.EaseInOutQuad
            );
        }

        public void UpdateCenterPosition(double maxWidth, TextAlignmentType type)
        {
            if (OriginalCanvasTextLayout == null)
            {
                return;
            }

            double centerY = OriginalPosition.Y + (OriginalCanvasTextLayout?.LayoutBounds.Height ?? 0) / 2;

            CenterPosition = type switch
            {
                TextAlignmentType.Left => new Vector2(OriginalPosition.X, (float)centerY),
                TextAlignmentType.Center => new Vector2((float)(OriginalPosition.X + maxWidth / 2.0), (float)centerY),
                TextAlignmentType.Right => new Vector2((float)(OriginalPosition.X + maxWidth), (float)centerY),
                _ => throw new System.ArgumentOutOfRangeException(nameof(type), type, null),
            };
        }

        public void DisposeTextLayout()
        {
            PhoneticCanvasTextLayout?.Dispose();
            PhoneticCanvasTextLayout = null;

            OriginalCanvasTextLayout?.Dispose();
            OriginalCanvasTextLayout = null;

            TranslatedCanvasTextLayout?.Dispose();
            TranslatedCanvasTextLayout = null;
        }

        public void RecreateTextLayout(
            ICanvasAnimatedControl control,
            bool createPhonetic, bool createTranslated,
            int phoneticTextFontSize, int originalTextFontSize, int translatedTextFontSize,
            LyricsFontWeight fontWeight,
            string fontFamilyCJK, string fontFamilyWestern,
            double maxWidth, double maxHeight, TextAlignmentType type)
        {
            DisposeTextLayout();

            if (createPhonetic && PhoneticText != "")
            {
                PhoneticCanvasTextLayout = new CanvasTextLayout(control, PhoneticText, new CanvasTextFormat
                {
                    HorizontalAlignment = CanvasHorizontalAlignment.Left,
                    VerticalAlignment = CanvasVerticalAlignment.Top,
                    FontSize = phoneticTextFontSize,
                    FontWeight = fontWeight.ToFontWeight(),
                }, (float)maxWidth, (float)maxHeight)
                {
                    HorizontalAlignment = type.ToCanvasHorizontalAlignment(),
                };
                PhoneticCanvasTextLayout.SetFontFamily(PhoneticText, fontFamilyCJK, fontFamilyWestern);
            }

            OriginalCanvasTextLayout = new CanvasTextLayout(control, OriginalText, new CanvasTextFormat
            {
                HorizontalAlignment = CanvasHorizontalAlignment.Left,
                VerticalAlignment = CanvasVerticalAlignment.Top,
                FontSize = originalTextFontSize,
                FontWeight = fontWeight.ToFontWeight(),
            }, (float)maxWidth, (float)maxHeight)
            {
                HorizontalAlignment = type.ToCanvasHorizontalAlignment()
            };
            OriginalCanvasTextLayout.SetFontFamily(OriginalText, fontFamilyCJK, fontFamilyWestern);

            if (createTranslated && TranslatedText != "")
            {
                TranslatedCanvasTextLayout = new CanvasTextLayout(control, TranslatedText, new CanvasTextFormat
                {
                    HorizontalAlignment = CanvasHorizontalAlignment.Left,
                    VerticalAlignment = CanvasVerticalAlignment.Top,
                    FontSize = translatedTextFontSize,
                    FontWeight = fontWeight.ToFontWeight(),
                }, (float)maxWidth, (float)maxHeight)
                {
                    HorizontalAlignment = type.ToCanvasHorizontalAlignment()
                };
                TranslatedCanvasTextLayout.SetFontFamily(TranslatedText, fontFamilyCJK, fontFamilyWestern);
            }
        }

        public void DisposeTextGeometry()
        {
            PhoneticCanvasGeometry?.Dispose();
            PhoneticCanvasGeometry = null;

            OriginalCanvasGeometry?.Dispose();
            OriginalCanvasGeometry = null;

            TranslatedCanvasGeometry?.Dispose();
            TranslatedCanvasGeometry = null;
        }

        public void RecreateTextGeometry()
        {
            DisposeTextGeometry();

            if (PhoneticCanvasTextLayout != null)
            {
                PhoneticCanvasGeometry = CanvasGeometry.CreateText(PhoneticCanvasTextLayout);
            }

            if (OriginalCanvasTextLayout != null)
            {
                OriginalCanvasGeometry = CanvasGeometry.CreateText(OriginalCanvasTextLayout);
            }

            if (TranslatedCanvasTextLayout != null)
            {
                TranslatedCanvasGeometry = CanvasGeometry.CreateText(TranslatedCanvasTextLayout);
            }
        }
    }
}
