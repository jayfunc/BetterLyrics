using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.WinUI3.Extensions;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace BetterLyrics.WinUI3.Models.Lyrics
{
    public class RenderLyricsLine : BaseRenderLyrics
    {
        public List<RenderLyricsChar> PrimaryRenderChars { get; private set; } = [];
        public List<RenderLyricsSyllable> PrimaryRenderSyllables { get; private set; }

        public double AnimationDuration { get; set; } = 0.3;

        public ValueTransition<double> AngleTransition { get; set; }
        public ValueTransition<double> BlurAmountTransition { get; set; }
        public ValueTransition<double> ScaleTransition { get; set; }

        public ValueTransition<double> PlayedPrimaryOpacityTransition { get; set; }
        public ValueTransition<double> UnplayedPrimaryOpacityTransition { get; set; }
        public ValueTransition<double> SecondaryOpacityTransition { get; set; }
        public ValueTransition<double> TertiaryOpacityTransition { get; set; }

        public ValueTransition<double> PrimaryXOffsetTransition { get; set; }
        public ValueTransition<double> SecondaryXOffsetTransition { get; set; }
        public ValueTransition<double> TertiaryXOffsetTransition { get; set; }

        public ValueTransition<double> OffsetTransition { get; set; }

        public ValueTransition<AppColor> PlayedFillColorTransition { get; set; }
        public ValueTransition<AppColor> UnplayedFillColorTransition { get; set; }
        public ValueTransition<AppColor> PlayedStrokeColorTransition { get; set; }
        public ValueTransition<AppColor> UnplayedStrokeColorTransition { get; set; }

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
        public Vector2 CenterPosition { get; set; }
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

        public CanvasCommandList? CachedStroke { get; private set; }
        public CanvasCommandList? CachedFill { get; private set; }

        public TintEffect? UnplayedFillTint { get; private set; }
        public TintEffect? UnplayedStrokeTint { get; private set; }
        public CompositeEffect? UnplayedComposite { get; private set; }

        public CanvasTextLayoutRegion[]? PrimaryTextRegions { get; private set; }

        public RenderLyricsRegion[]? RenderLyricsRegions { get; private set; }

        /// <summary>
        /// 轨道索引 (0 = 主轨道, 1 = 第一副轨道, etc.)
        /// 用于布局计算时的堆叠逻辑
        /// </summary>
        public int LaneIndex { get; set; } = 0;

        public double? PrimaryLineHeight => PrimaryRenderChars.FirstOrDefault()?.LayoutRect.Height;

        public bool IsPrimaryHasRealSyllableInfo { get; set; }

        public string AgentId { get; set; }

        public TextAlignmentType? HorizontalAlignmentType { get; set; }

        public RenderLyricsLine(LyricsLine lyricsLine) : base(lyricsLine)
        {
            AngleTransition = new(
                initialValue: 0,
                EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
                defaultTotalDuration: AnimationDuration
            );
            BlurAmountTransition = new(
                 initialValue: 0,
                 EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
                 defaultTotalDuration: AnimationDuration
             );
            TertiaryOpacityTransition = new(
                initialValue: 0,
                EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
                defaultTotalDuration: AnimationDuration
            );
            PlayedPrimaryOpacityTransition = new(
                initialValue: 0,
                EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
                defaultTotalDuration: AnimationDuration
            );
            UnplayedPrimaryOpacityTransition = new(
                initialValue: 0,
                EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
                defaultTotalDuration: AnimationDuration
            );
            SecondaryOpacityTransition = new(
                initialValue: 0,
                EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
                defaultTotalDuration: AnimationDuration
            );
            ScaleTransition = new(
                initialValue: 1.0,
                EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
                defaultTotalDuration: AnimationDuration
            );
            PrimaryXOffsetTransition = new(
                initialValue: 0,
                EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
                defaultTotalDuration: AnimationDuration
            );
            SecondaryXOffsetTransition = new(
                initialValue: 0,
                EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
                defaultTotalDuration: AnimationDuration
            );
            TertiaryXOffsetTransition = new(
                initialValue: 0,
                EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
                defaultTotalDuration: AnimationDuration
            );
            OffsetTransition = new(
                initialValue: 0,
                EasingHelper.GetInterpolatorByEasingType<double>(EasingType.Sine),
                defaultTotalDuration: AnimationDuration
            );
            PlayedFillColorTransition = new(
                initialValue: Colors.Transparent,
                defaultTotalDuration: 0.3f,
                interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to)
            );
            UnplayedFillColorTransition = new(
                initialValue: Colors.Transparent,
                defaultTotalDuration: 0.3f,
                interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to)
            );
            PlayedStrokeColorTransition = new(
                initialValue: Colors.Transparent,
                defaultTotalDuration: 0.3f,
                interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to)
            );
            UnplayedStrokeColorTransition = new(
                initialValue: Colors.Transparent,
                defaultTotalDuration: 0.3f,
                interpolator: (from, to, progress) => ColorHelper.GetInterpolatedColor(progress, from, to)
            );

            StartMs = lyricsLine.StartMs;
            EndMs = lyricsLine.EndMs;
            TertiaryText = lyricsLine.TertiaryText;
            PrimaryText = lyricsLine.PrimaryText;
            SecondaryText = lyricsLine.SecondaryText;
            PrimaryRenderSyllables = lyricsLine.PrimarySyllables.Select(x => new RenderLyricsSyllable(x)).ToList();
            IsPrimaryHasRealSyllableInfo = lyricsLine.IsPrimaryHasRealSyllableInfo;
            AgentId = lyricsLine.AgentId;
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
            double maxWidth, double maxHeight,
            TextAlignmentType type, bool autoWrap, LyricsLayoutOrientation orientation)
        {
            DisposeTextLayout();

            var wordWrapping = autoWrap ? CanvasWordWrapping.Wrap : CanvasWordWrapping.NoWrap;
            var horizontalAlignment = type.ToCanvasHorizontalAlignment();

            bool phoneticVisible = createPhonetic && !string.IsNullOrWhiteSpace(TertiaryText);
            bool translatedVisible = createTranslated && !string.IsNullOrWhiteSpace(SecondaryText);

            var verticalAlignment = CanvasVerticalAlignment.Top;
            var canvasTextDirection = orientation == LyricsLayoutOrientation.Vertical ? CanvasTextDirection.TopToBottomThenRightToLeft : CanvasTextDirection.LeftToRightThenTopToBottom;

            // 音译
            if (phoneticVisible)
            {
                TertiaryTextLayout = new CanvasTextLayout(control, TertiaryText, new CanvasTextFormat
                {
                    VerticalAlignment = verticalAlignment,
                    FontSize = phoneticTextFontSize,
                    FontWeight = fontWeight.ToFontWeight(),
                    WordWrapping = wordWrapping,
                }, (float)maxWidth, (float)maxHeight)
                {
                    HorizontalAlignment = horizontalAlignment,
                    Options = CanvasDrawTextOptions.NoPixelSnap,
                    Direction = canvasTextDirection,
                };
                TertiaryTextLayout.SetFontFamily(TertiaryText, fontFamilyCJK, fontFamilyWestern);
            }

            // 原文
            PrimaryTextLayout = new CanvasTextLayout(control, PrimaryText, new CanvasTextFormat
            {
                VerticalAlignment = verticalAlignment,
                FontSize = originalTextFontSize,
                FontWeight = fontWeight.ToFontWeight(),
                WordWrapping = wordWrapping,
            }, (float)maxWidth, (float)maxHeight)
            {
                HorizontalAlignment = horizontalAlignment,
                Options = CanvasDrawTextOptions.NoPixelSnap,
                Direction = canvasTextDirection,
            };
            PrimaryTextLayout.SetFontFamily(PrimaryText, fontFamilyCJK, fontFamilyWestern);
            PrimaryTextRegions = PrimaryTextLayout.GetCharacterRegions(0, PrimaryText.Length);

            // 翻译
            if (translatedVisible)
            {
                SecondaryTextLayout = new CanvasTextLayout(control, SecondaryText, new CanvasTextFormat
                {
                    VerticalAlignment = verticalAlignment,
                    FontSize = translatedTextFontSize,
                    FontWeight = fontWeight.ToFontWeight(),
                    WordWrapping = wordWrapping,
                }, (float)maxWidth, (float)maxHeight)
                {
                    HorizontalAlignment = horizontalAlignment,
                    Options = CanvasDrawTextOptions.NoPixelSnap,
                    Direction = canvasTextDirection,
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

        public void RecreateRenderChars(int strokeWidth)
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
                var bounds = region.LayoutBounds.Extend(
                    startCharIndex == 0 ? strokeWidth : strokeWidth / 4f,
                    strokeWidth / 2f,
                    startCharIndex == textLength - 1 ? strokeWidth : strokeWidth / 4f,
                    strokeWidth / 2f);

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

        public void EnsureCaches(ICanvasResourceCreator resourceCreator, double strokeWidth)
        {
            if (CachedStroke != null && CachedFill != null) return;

            // 缓存纯白色的填充（作为 Fill Mask）
            CachedFill = new CanvasCommandList(resourceCreator);
            using (var ds = CachedFill.CreateDrawingSession())
            {
                if (TertiaryTextLayout != null) ds.DrawTextLayout(TertiaryTextLayout, TertiaryPosition, ColorExtensions.FromAppColor(Colors.White));
                if (PrimaryTextLayout != null) ds.DrawTextLayout(PrimaryTextLayout, PrimaryPosition, ColorExtensions.FromAppColor(Colors.White));
                if (SecondaryTextLayout != null) ds.DrawTextLayout(SecondaryTextLayout, SecondaryPosition, ColorExtensions.FromAppColor(Colors.White));
            }

            CachedStroke = new CanvasCommandList(resourceCreator);

            // 缓存纯白色的描边（作为 Stroke Mask）
            if (strokeWidth > 0)
            {
                using var roundStrokeStyle = new CanvasStrokeStyle
                {
                    LineJoin = CanvasLineJoin.Round,
                    StartCap = CanvasCapStyle.Round,
                    EndCap = CanvasCapStyle.Round,
                };
                using var ds = CachedStroke.CreateDrawingSession();
                if (TertiaryCanvasGeometry != null) ds.DrawGeometry(TertiaryCanvasGeometry, TertiaryPosition, ColorExtensions.FromAppColor(Colors.White), (float)strokeWidth, roundStrokeStyle);
                if (PrimaryCanvasGeometry != null) ds.DrawGeometry(PrimaryCanvasGeometry, PrimaryPosition, ColorExtensions.FromAppColor(Colors.White), (float)strokeWidth, roundStrokeStyle);
                if (SecondaryCanvasGeometry != null) ds.DrawGeometry(SecondaryCanvasGeometry, SecondaryPosition, ColorExtensions.FromAppColor(Colors.White), (float)strokeWidth, roundStrokeStyle);
            }

            UnplayedFillTint = new TintEffect { Source = CachedFill, Color = ColorExtensions.FromAppColor(Colors.White) };
            UnplayedStrokeTint = new TintEffect { Source = CachedStroke, Color = ColorExtensions.FromAppColor(Colors.White) };
            UnplayedComposite = new CompositeEffect { Sources = { UnplayedStrokeTint, UnplayedFillTint }, Mode = CanvasComposite.SourceOver };

            if (PrimaryTextRegions != null && (RenderLyricsRegions == null || RenderLyricsRegions.Length != PrimaryTextRegions.Length))
            {
                DisposeRenderLyricsRegions();
                RenderLyricsRegions = new RenderLyricsRegion[PrimaryTextRegions.Length];
                for (int i = 0; i < PrimaryTextRegions.Length; i++)
                {
                    RenderLyricsRegions[i] = new RenderLyricsRegion(CachedFill, CachedStroke);
                }
            }
        }

        private void DisposePrimaryRenderCharsEffects()
        {
            foreach (var cache in PrimaryRenderChars)
            {
                cache?.DisposeEffetcts();
            }
        }

        private void DisposeRenderLyricsRegions()
        {
            if (RenderLyricsRegions != null)
            {
                foreach (var region in RenderLyricsRegions)
                {
                    region?.Dispose();
                }
                RenderLyricsRegions = null;
            }
        }

        public void DisposeCaches()
        {
            UnplayedComposite?.Dispose();
            UnplayedStrokeTint?.Dispose();
            UnplayedFillTint?.Dispose();
            CachedStroke?.Dispose();
            CachedFill?.Dispose();

            UnplayedComposite = null;
            UnplayedStrokeTint = null;
            UnplayedFillTint = null;
            CachedStroke = null;
            CachedFill = null;

            DisposeRenderLyricsRegions();
            DisposePrimaryRenderCharsEffects();
        }

        public void Update(TimeSpan elapsedTime)
        {
            AngleTransition.Update(elapsedTime);
            ScaleTransition.Update(elapsedTime);
            BlurAmountTransition.Update(elapsedTime);

            PlayedPrimaryOpacityTransition.Update(elapsedTime);
            UnplayedPrimaryOpacityTransition.Update(elapsedTime);
            SecondaryOpacityTransition.Update(elapsedTime);
            TertiaryOpacityTransition.Update(elapsedTime);

            PrimaryXOffsetTransition.Update(elapsedTime);
            SecondaryXOffsetTransition.Update(elapsedTime);
            TertiaryXOffsetTransition.Update(elapsedTime);
            OffsetTransition.Update(elapsedTime);

            PlayedFillColorTransition.Update(elapsedTime);
            UnplayedFillColorTransition.Update(elapsedTime);
            PlayedStrokeColorTransition.Update(elapsedTime);
            UnplayedStrokeColorTransition.Update(elapsedTime);
        }

    }
}
