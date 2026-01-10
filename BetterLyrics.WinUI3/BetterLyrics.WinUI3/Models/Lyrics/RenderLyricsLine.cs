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
using Windows.UI;

namespace BetterLyrics.WinUI3.Models.Lyrics
{
    public class RenderLyricsLine : BaseRenderLyrics
    {
        public List<RenderLyricsChar> PrimaryRenderChars { get; private set; } = [];
        public List<RenderLyricsSyllable> PrimaryRenderSyllables { get; private set; }

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

        public CanvasTextLayout? PrimaryTextLayout { get; private set; }
        public CanvasTextLayout? SecondaryTextLayout { get; private set; }
        public CanvasTextLayout? TertiaryTextLayout { get; private set; }

        /// <summary>
        /// 原文坐标（相对于坐标原点）
        /// </summary>
        public Vector2 PrimaryPosition { get; set; }
        /// <summary>
        /// 译文坐标（相对于坐标原点）
        /// </summary>
        public Vector2 SecondaryPosition { get; set; }
        /// <summary>
        /// 注音坐标（相对于坐标原点）
        /// </summary>
        public Vector2 TertiaryPosition { get; set; }

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

        public CanvasGeometry? PrimaryCanvasGeometry { get; private set; }
        public CanvasGeometry? SecondaryCanvasGeometry { get; private set; }
        public CanvasGeometry? TertiaryCanvasGeometry { get; private set; }

        public string PrimaryText { get; set; } = "";
        public string SecondaryText { get; set; } = "";
        public string TertiaryText { get; set; } = "";

        /// <summary>
        /// 轨道索引 (0 = 主轨道, 1 = 第一副轨道, etc.)
        /// 用于布局计算时的堆叠逻辑
        /// </summary>
        public int LaneIndex { get; set; } = 0;

        public double? PrimaryLineHeight => PrimaryRenderChars.FirstOrDefault()?.LayoutRect.Height;

        public bool IsPrimaryHasRealSyllableInfo { get; set; }

        public RenderLyricsLine(LyricsLine lyricsLine) : base(lyricsLine)
        {
            AngleTransition = new(
                initialValue: 0,
                defaultTotalDuration: AnimationDuration,
                defaultEasingType: EasingType.EaseInOutSine
            );
            BlurAmountTransition = new(
                 initialValue: 0,
                 defaultTotalDuration: AnimationDuration,
                 defaultEasingType: EasingType.EaseInOutSine
             );
            PhoneticOpacityTransition = new(
                initialValue: 0,
                defaultTotalDuration: AnimationDuration,
                defaultEasingType: EasingType.EaseInOutSine
            );
            PlayedOriginalOpacityTransition = new(
                initialValue: 0,
                defaultTotalDuration: AnimationDuration,
                defaultEasingType: EasingType.EaseInOutSine
            );
            UnplayedOriginalOpacityTransition = new(
                initialValue: 0,
                defaultTotalDuration: AnimationDuration,
                defaultEasingType: EasingType.EaseInOutSine
            );
            TranslatedOpacityTransition = new(
                initialValue: 0,
                defaultTotalDuration: AnimationDuration,
                defaultEasingType: EasingType.EaseInOutSine
            );
            ScaleTransition = new(
                initialValue: 0,
                defaultTotalDuration: AnimationDuration,
                defaultEasingType: EasingType.EaseInOutSine
            );
            YOffsetTransition = new(
                initialValue: 0,
                defaultTotalDuration: AnimationDuration,
                defaultEasingType: EasingType.EaseInOutSine
            );
            ColorTransition = new(
                initialValue: Colors.Transparent,
                defaultTotalDuration: 0.3f,
                interpolator: (from, to, progress) => Helper.ColorHelper.GetInterpolatedColor(progress, from, to)
            );

            StartMs = lyricsLine.StartMs;
            EndMs = lyricsLine.EndMs;
            TertiaryText = lyricsLine.TertiaryText;
            PrimaryText = lyricsLine.PrimaryText;
            SecondaryText = lyricsLine.SecondaryText;
            PrimaryRenderSyllables = lyricsLine.PrimarySyllables.Select(x => new RenderLyricsSyllable(x)).ToList();
            IsPrimaryHasRealSyllableInfo = lyricsLine.IsPrimaryHasRealSyllableInfo;
        }

        public void UpdateCenterPosition(double maxWidth, TextAlignmentType type)
        {
            if (PrimaryTextLayout == null)
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
            TertiaryTextLayout?.Dispose();
            TertiaryTextLayout = null;

            PrimaryTextLayout?.Dispose();
            PrimaryTextLayout = null;

            SecondaryTextLayout?.Dispose();
            SecondaryTextLayout = null;
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

            if (createPhonetic && TertiaryText != "")
            {
                TertiaryTextLayout = new CanvasTextLayout(control, TertiaryText, new CanvasTextFormat
                {
                    HorizontalAlignment = CanvasHorizontalAlignment.Left,
                    VerticalAlignment = CanvasVerticalAlignment.Top,
                    FontSize = phoneticTextFontSize,
                    FontWeight = fontWeight.ToFontWeight(),
                }, (float)maxWidth, (float)maxHeight)
                {
                    HorizontalAlignment = type.ToCanvasHorizontalAlignment(),
                };
                TertiaryTextLayout.SetFontFamily(TertiaryText, fontFamilyCJK, fontFamilyWestern);
            }

            PrimaryTextLayout = new CanvasTextLayout(control, PrimaryText, new CanvasTextFormat
            {
                HorizontalAlignment = CanvasHorizontalAlignment.Left,
                VerticalAlignment = CanvasVerticalAlignment.Top,
                FontSize = originalTextFontSize,
                FontWeight = fontWeight.ToFontWeight(),
            }, (float)maxWidth, (float)maxHeight)
            {
                HorizontalAlignment = type.ToCanvasHorizontalAlignment()
            };
            PrimaryTextLayout.SetFontFamily(PrimaryText, fontFamilyCJK, fontFamilyWestern);

            if (createTranslated && SecondaryText != "")
            {
                SecondaryTextLayout = new CanvasTextLayout(control, SecondaryText, new CanvasTextFormat
                {
                    HorizontalAlignment = CanvasHorizontalAlignment.Left,
                    VerticalAlignment = CanvasVerticalAlignment.Top,
                    FontSize = translatedTextFontSize,
                    FontWeight = fontWeight.ToFontWeight(),
                }, (float)maxWidth, (float)maxHeight)
                {
                    HorizontalAlignment = type.ToCanvasHorizontalAlignment()
                };
                SecondaryTextLayout.SetFontFamily(SecondaryText, fontFamilyCJK, fontFamilyWestern);
            }
        }

        public void DisposeTextGeometry()
        {
            TertiaryCanvasGeometry?.Dispose();
            TertiaryCanvasGeometry = null;

            PrimaryCanvasGeometry?.Dispose();
            PrimaryCanvasGeometry = null;

            SecondaryCanvasGeometry?.Dispose();
            SecondaryCanvasGeometry = null;
        }

        public void RecreateTextGeometry()
        {
            DisposeTextGeometry();

            if (TertiaryTextLayout != null)
            {
                TertiaryCanvasGeometry = CanvasGeometry.CreateText(TertiaryTextLayout);
            }

            if (PrimaryTextLayout != null)
            {
                PrimaryCanvasGeometry = CanvasGeometry.CreateText(PrimaryTextLayout);
            }

            if (SecondaryTextLayout != null)
            {
                SecondaryCanvasGeometry = CanvasGeometry.CreateText(SecondaryTextLayout);
            }
        }

        public void RecreateRenderChars()
        {
            PrimaryRenderChars.Clear();
            if (PrimaryTextLayout == null) return;

            foreach (var syllable in PrimaryRenderSyllables)
            {
                syllable.ChildrenRenderLyricsChars.Clear();
            }

            var textLength = PrimaryText.Length;

            for (int startCharIndex = 0; startCharIndex < textLength; startCharIndex++)
            {
                var region = PrimaryTextLayout.GetCharacterRegions(startCharIndex, 1).FirstOrDefault();
                var bounds = region.LayoutBounds;

                var syllable = PrimaryRenderSyllables.FirstOrDefault(x => x.StartIndex <= startCharIndex && startCharIndex <= x.EndIndex);
                if (syllable == null) continue;

                var avgCharDuration = syllable.DurationMs / syllable.Length;
                var charStartMs = syllable.StartMs + (startCharIndex - syllable.StartIndex) * avgCharDuration;
                var charEndMs = charStartMs + avgCharDuration;

                var renderLyricsChar = new RenderLyricsChar(new BaseLyrics
                {
                    StartIndex = startCharIndex,
                    Text = PrimaryText[startCharIndex].ToString(),
                    StartMs = charStartMs,
                    EndMs = charEndMs,
                }, bounds);

                syllable.ChildrenRenderLyricsChars.Add(renderLyricsChar);

                PrimaryRenderChars.Add(renderLyricsChar);
            }
        }

        public void Update(TimeSpan elapsedTime)
        {
            AngleTransition.Update(elapsedTime);
            ScaleTransition.Update(elapsedTime);
            BlurAmountTransition.Update(elapsedTime);
            PhoneticOpacityTransition.Update(elapsedTime);
            PlayedOriginalOpacityTransition.Update(elapsedTime);
            UnplayedOriginalOpacityTransition.Update(elapsedTime);
            TranslatedOpacityTransition.Update(elapsedTime);
            YOffsetTransition.Update(elapsedTime);
            ColorTransition.Update(elapsedTime);
        }

    }
}
