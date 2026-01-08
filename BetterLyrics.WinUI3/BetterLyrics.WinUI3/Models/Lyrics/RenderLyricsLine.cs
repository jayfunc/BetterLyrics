using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Windows.Documents;
using Windows.Foundation;
using Windows.UI;

namespace BetterLyrics.WinUI3.Models.Lyrics
{
    public class RenderLyricsLine : LyricsLine
    {
        public List<RenderLyricsChar> RenderLyricsOriginalChars { get; set; } = [];

        public double AnimationDuration { get; set; } = 0.3;

        public ValueTransition<double> AngleTransition { get; set; }
        public ValueTransition<double> BlurAmountTransition { get; set; }
        public ValueTransition<double> PhoneticOpacityTransition { get; set; }
        public ValueTransition<double> PlayedOriginalOpacityTransition { get; set; }
        public ValueTransition<double> UnplayedOriginalOpacityTransition { get; set; }
        public ValueTransition<double> TranslatedOpacityTransition { get; set; }
        public ValueTransition<double> ScaleTransition { get; set; }
        public ValueTransition<double> YOffsetTransition { get; set; }
        public ValueTransition<Color> ColorTransition { get; set; }

        public CanvasTextLayout? OriginalCanvasTextLayout { get; private set; }
        public CanvasTextLayout? TranslatedCanvasTextLayout { get; private set; }
        public CanvasTextLayout? PhoneticCanvasTextLayout { get; private set; }

        /// <summary>
        /// 原文坐标（相对于坐标原点）
        /// </summary>
        public Vector2 OriginalPosition { get; set; }
        /// <summary>
        /// 译文坐标（相对于坐标原点）
        /// </summary>
        public Vector2 TranslatedPosition { get; set; }
        /// <summary>
        /// 注音坐标（相对于坐标原点）
        /// </summary>
        public Vector2 PhoneticPosition { get; set; }

        /// <summary>
        /// 顶部坐标（相对于坐标原点）
        /// </summary>
        public Vector2 TopLeftPosition { get; set; }
        /// <summary>
        /// 中心坐标（相对于坐标原点）
        /// </summary>
        public Vector2 CenterPosition { get; private set; }
        /// <summary>
        /// 底部坐标（相对于坐标原点）
        /// </summary>
        public Vector2 BottomRightPosition { get; set; }

        public CanvasGeometry? OriginalCanvasGeometry { get; private set; }
        public CanvasGeometry? TranslatedCanvasGeometry { get; private set; }
        public CanvasGeometry? PhoneticCanvasGeometry { get; private set; }

        /// <summary>
        /// 轨道索引 (0 = 主轨道, 1 = 第一副轨道, etc.)
        /// 用于布局计算时的堆叠逻辑
        /// </summary>
        public int LaneIndex { get; set; } = 0;
        /// <summary>
        /// 是否为背景人声/和声
        /// </summary>
        public bool IsPlayingLastFrame { get; set; } = false;

        public RenderLyricsLine()
        {
            AngleTransition = new(
                initialValue: 0,
                durationSeconds: AnimationDuration,
                easingType: EasingType.EaseInOutSine
            );
            BlurAmountTransition = new(
                 initialValue: 0,
                 durationSeconds: AnimationDuration,
                 easingType: EasingType.EaseInOutSine
             );
            PhoneticOpacityTransition = new(
                initialValue: 0,
                durationSeconds: AnimationDuration,
                easingType: EasingType.EaseInOutSine
            );
            PlayedOriginalOpacityTransition = new(
                initialValue: 0,
                durationSeconds: AnimationDuration,
                easingType: EasingType.EaseInOutSine
            );
            UnplayedOriginalOpacityTransition = new(
                initialValue: 0,
                durationSeconds: AnimationDuration,
                easingType: EasingType.EaseInOutSine
            );
            TranslatedOpacityTransition = new(
                initialValue: 0,
                durationSeconds: AnimationDuration,
                easingType: EasingType.EaseInOutSine
            );
            ScaleTransition = new(
                initialValue: 0,
                durationSeconds: AnimationDuration,
                easingType: EasingType.EaseInOutSine
            );
            YOffsetTransition = new(
                initialValue: 0,
                durationSeconds: AnimationDuration,
                easingType: EasingType.EaseInOutSine
            );
            ColorTransition = new(
                initialValue: Colors.Transparent,
                durationSeconds: 0.3f,
                interpolator: (from, to, progress) => Helper.ColorHelper.GetInterpolatedColor(progress, from, to)
            );
        }

        public void UpdateCenterPosition(double maxWidth, TextAlignmentType type)
        {
            if (OriginalCanvasTextLayout == null)
            {
                return;
            }

            double centerY = (TopLeftPosition.Y + BottomRightPosition.Y) / 2;

            CenterPosition = type switch
            {
                TextAlignmentType.Left => new Vector2(0, (float)centerY),
                TextAlignmentType.Center => new Vector2((float)(0 + maxWidth / 2.0), (float)centerY),
                TextAlignmentType.Right => new Vector2((float)(0 + maxWidth), (float)centerY),
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

        public void RecalculateCharacterGeometries()
        {
            RenderLyricsOriginalChars.Clear();
            if (OriginalCanvasTextLayout == null) return;

            var textLength = OriginalText.Length;

            for (int i = 0; i < textLength; i++)
            {
                var region = OriginalCanvasTextLayout.GetCharacterRegions(i, 1).FirstOrDefault();
                var bounds = region.LayoutBounds;

                RenderLyricsOriginalChars.Add(new RenderLyricsChar()
                {
                    Index = i,
                    LayoutRect = bounds,
                    Text = OriginalText[i].ToString()
                });
            }
        }

    }
}
