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

        /// <summary>
        /// 背景文字层（底字）
        /// </summary>
        public CanvasCommandList? BackgroundFontEffect { get; private set; }

        /// <summary>
        /// 背景层
        /// </summary>
        public OpacityEffect? BackgroundEffect { get; private set; }

        /// <summary>
        /// 辉光层
        /// </summary>
        public GaussianBlurEffect? ForegroundBlurEffect { get; private set; }

        /// <summary>
        /// 高亮层
        /// </summary>
        public AlphaMaskEffect? ForegroundHighlightEffect { get; private set; }

        /// <summary>
        /// 前景文字层
        /// </summary>
        public CanvasCommandList? ForegroundFontEffect { get; private set; }

        public CanvasCommandList? ComposedLineEffect { get; private set; }

        public CanvasCommandList? CurrentCharMask { get; private set; }
        public CanvasCommandList? LineStartToCurrentCharMask { get; private set; }
        public CanvasCommandList? CurrentLineMask { get; private set; }

        public CanvasCommandList? PlaceholderEffect { get; private set; }

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

        public void RecreateTextLayout(ICanvasAnimatedControl control, CanvasTextFormat textFormat, double maxWidth, double maxHeight, TextAlignmentType type)
        {
            CanvasTextLayout?.Dispose();
            CanvasTextLayout = null;
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

        public void DisposeFontEffects()
        {
            BackgroundFontEffect?.Dispose();
            BackgroundFontEffect = null;
            ForegroundFontEffect?.Dispose();
            ForegroundFontEffect = null;
        }

        public void RecreateFontEffect(ICanvasAnimatedControl control, Color strokeColor, int strokeWidth, Color bgFontColor, Color fgFontColor)
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
            // 大于 0 才描边，避免不必要的资源浪费
            if (strokeWidth > 0)
            {
                bgFontEffectDs.DrawGeometry(TextGeometry, Position, strokeColor, strokeWidth); // 描边
                fgFontEffectDs.DrawGeometry(TextGeometry, Position, strokeColor, strokeWidth); // 描边
            }
            bgFontEffectDs.FillGeometry(TextGeometry, Position, bgFontColor); // 填充
            fgFontEffectDs.FillGeometry(TextGeometry, Position, fgFontColor); // 填充
        }

        /// <summary>
        /// 背景层
        /// </summary>
        /// <param name="lyricsLayerOpacity">_lyricsOpacityTransition.Value</param>
        public void RecreateBackgroundEffect(double lyricsLayerOpacity)
        {
            BackgroundEffect?.Dispose();
            BackgroundEffect = null;
            if (BackgroundFontEffect == null)
            {
                return;
            }
            BackgroundEffect = new OpacityEffect
            {
                Source = new GaussianBlurEffect
                {
                    Source = BackgroundFontEffect,
                    BlurAmount = (float)BlurAmountTransition.Value,
                    BorderMode = EffectBorderMode.Soft,
                    Optimization = EffectOptimization.Speed,
                },
                Opacity = (float)(OpacityTransition.Value * lyricsLayerOpacity),
            };
        }

        public void UpdateBackgroundEffect(double lyricsLayerOpacity)
        {
            BackgroundEffect?.Opacity = (float)(OpacityTransition.Value * lyricsLayerOpacity);
            GaussianBlurEffect? blurEffect = (GaussianBlurEffect?)(BackgroundEffect?.Source);
            blurEffect?.BlurAmount = (float)BlurAmountTransition.Value;
        }

        private IGraphicsEffectSource GetAlphaMask(ICanvasAnimatedControl control, LineRenderingType lineRenderingType)
        {
            if (PlaceholderEffect == null)
            {
                RecreatePlaceholder(control);
            }

            var result = lineRenderingType switch
            {
                LineRenderingType.CurrentChar => CurrentCharMask,
                LineRenderingType.LineStartToCurrentChar => LineStartToCurrentCharMask,
                // Here, cuz AlphaMask only takes care of alpha channel
                // so ForegroundFontEffect can be a mask for CurrentLine
                // And we don't need to create a new mask for CurrentLine
                LineRenderingType.CurrentLine => CurrentLineMask,
                _ => PlaceholderEffect
            };
            return result ?? PlaceholderEffect!;
        }

        /// <summary>
        /// 销毁并重新创建辉光效果层
        /// 仅需在布局重构 (Relayout) 时调用
        /// </summary>
        /// <param name="lineRenderingType">_lyricsGlowEffectScope</param>
        /// <param name="glowEffectAmount">_lyricsGlowEffectAmount</param>
        public void RecreateForegroundBlurEffect(ICanvasAnimatedControl control, LineRenderingType lineRenderingType, double glowEffectAmount)
        {
            ForegroundBlurEffect?.Dispose();
            ForegroundBlurEffect = null;
            if (ForegroundFontEffect == null)
            {
                return;
            }
            var mask = GetAlphaMask(control, lineRenderingType);
            if (mask == null)
            {
                return;
            }
            ForegroundBlurEffect = new GaussianBlurEffect
            {
                Source = new AlphaMaskEffect
                {
                    Source = ForegroundFontEffect,
                    AlphaMask = mask,
                },
                BlurAmount = (float)glowEffectAmount,
                Optimization = EffectOptimization.Speed,
            };
        }

        /// <summary>
        /// 仅当前行需要调用此方法（每次 Update 都调用一次）
        /// </summary>
        /// <param name="control"></param>
        /// <param name="lineRenderingType"></param>
        /// <param name="glowEffectAmount"></param>
        public void UpdateForegroundBlurEffect(ICanvasAnimatedControl control, LineRenderingType lineRenderingType, double glowEffectAmount)
        {
            if (ForegroundBlurEffect == null)
            {
                return;
            }
            if (ForegroundFontEffect == null)
            {
                return;
            }
            var mask = GetAlphaMask(control, lineRenderingType);
            if (mask == null)
            {
                return;
            }
            ForegroundBlurEffect.BlurAmount = (float)glowEffectAmount;
            var alphaMaskEffect = (AlphaMaskEffect)ForegroundBlurEffect.Source;
            alphaMaskEffect.Source = ForegroundFontEffect;
            alphaMaskEffect.AlphaMask = mask;
        }

        /// <summary>
        /// 销毁并重新创建高亮效果层
        /// 仅需在布局重构 (Relayout) 时调用
        /// </summary>
        /// <param name="control"></param>
        /// <param name="lineRenderingType"></param>
        public void RecreateForegroundHighlightEffect(ICanvasAnimatedControl control, LineRenderingType lineRenderingType)
        {
            ForegroundHighlightEffect?.Dispose();
            ForegroundHighlightEffect = null;

            if (ForegroundFontEffect == null)
            {
                return;
            }

            var mask = GetAlphaMask(control, lineRenderingType);
            if (mask == null)
            {
                return;
            }

            ForegroundHighlightEffect = new AlphaMaskEffect
            {
                Source = ForegroundFontEffect,
                AlphaMask = mask,
            };
        }

        /// <summary>
        /// 仅当前行需要调用此方法（每次 Update 都调用一次）
        /// </summary>
        /// <param name="control"></param>
        /// <param name="lineRenderingType"></param>
        public void UpdateForegroundHighlightEffect(ICanvasAnimatedControl control, LineRenderingType lineRenderingType)
        {
            if (ForegroundHighlightEffect == null)
            {
                return;
            }

            if (ForegroundFontEffect == null)
            {
                return;
            }

            var mask = GetAlphaMask(control, lineRenderingType);
            if (mask == null)
            {
                return;
            }

            ForegroundHighlightEffect.Source = ForegroundFontEffect;
            ForegroundHighlightEffect.AlphaMask = mask;
        }

        /// <summary>
        /// 仅当前播放行需要调用此方法（每次 Update 都调用一次）
        /// </summary>
        /// <param name="control"></param>
        /// <param name="playingLineIndex"></param>
        /// <param name="charStartIndex"></param>
        /// <param name="charLength"></param>
        /// <param name="charProgress"></param>
        public void RecreateCurrentCharMask(ICanvasAnimatedControl control, int charStartIndex, int charLength, double charProgress)
        {
            CurrentCharMask?.Dispose();
            CurrentCharMask = null;
            CurrentCharMask = new CanvasCommandList(control);

            if (CanvasTextLayout == null)
            {
                return;
            }

            using var ds = CurrentCharMask.CreateDrawingSession();

            var highlightRegion = CanvasTextLayout
                .GetCharacterRegions(charStartIndex, charLength)
                .FirstOrDefault();

            double highlightTotalWidth = (double)highlightRegion.LayoutBounds.Width;
            // Draw the highlight for the current character
            double highlightWidth = highlightTotalWidth * charProgress;

            double fadingWidth = (double)highlightRegion.LayoutBounds.Height / 2;

            // Rects
            var highlightRect = new Rect(
                highlightRegion.LayoutBounds.X,
                highlightRegion.LayoutBounds.Y + Position.Y,
                highlightWidth,
                highlightRegion.LayoutBounds.Height
            );

            var fadeInRect = new Rect(
                highlightRect.Right - fadingWidth,
                highlightRegion.LayoutBounds.Y + Position.Y,
                fadingWidth,
                highlightRegion.LayoutBounds.Height
            );
            var fadeOutRect = new Rect(
                highlightRect.Right,
                highlightRegion.LayoutBounds.Y + Position.Y,
                fadingWidth,
                highlightRegion.LayoutBounds.Height
            );

            // Brushes
            using var fadeInBrush = CanvasHelper.CreateHorizontalFillBrush(
                control,
                [(0f, 0f), (1f, 1f)],
                (double)highlightRect.Right - fadingWidth,
                fadingWidth
            );
            using var fadeOutBrush = CanvasHelper.CreateHorizontalFillBrush(
                control,
                [(0f, 1f), (1f, 0f)],
                (double)highlightRect.Right,
                fadingWidth
            );

            ds.FillRectangle(fadeInRect, fadeInBrush);
            ds.FillRectangle(fadeOutRect, fadeOutBrush);
        }

        /// <summary>
        /// 仅当前播放行需要调用此方法（每次 Update 都调用一次）
        /// </summary>
        /// <param name="control"></param>
        /// <param name="playingLineIndex"></param>
        /// <param name="charStartIndex"></param>
        /// <param name="charLength"></param>
        /// <param name="charProgress"></param>
        public void RecreateLineStartToCurrentCharMask(ICanvasAnimatedControl control, int charStartIndex, int charLength, double charProgress)
        {
            LineStartToCurrentCharMask?.Dispose();
            LineStartToCurrentCharMask = null;
            LineStartToCurrentCharMask = new CanvasCommandList(control);

            if (CanvasTextLayout == null)
            {
                return;
            }

            using var ds = LineStartToCurrentCharMask.CreateDrawingSession();

            var regions = CanvasTextLayout.GetCharacterRegions(0, charStartIndex);
            var highlightRegion = CanvasTextLayout
                .GetCharacterRegions(charStartIndex, charLength)
                .FirstOrDefault();
            if (regions.Length > 0)
            {
                // Draw the mask for the current line
                for (int j = 0; j < regions.Length; j++)
                {
                    var region = regions[j];
                    var rect = new Rect(
                        region.LayoutBounds.X,
                        region.LayoutBounds.Y + Position.Y,
                        region.LayoutBounds.Width,
                        region.LayoutBounds.Height
                    );
                    ds.FillRectangle(rect, Color.FromArgb(255, 128, 128, 128));
                }
            }

            double highlightTotalWidth = (double)highlightRegion.LayoutBounds.Width;
            // Draw the highlight for the current character
            double highlightWidth = highlightTotalWidth * charProgress;

            double fadingWidth = (double)highlightRegion.LayoutBounds.Height / 2;

            // Rects
            var highlightRect = new Rect(
                highlightRegion.LayoutBounds.X,
                highlightRegion.LayoutBounds.Y + Position.Y,
                highlightWidth,
                highlightRegion.LayoutBounds.Height
            );

            var fadeInRect = new Rect(
                highlightRect.Right - fadingWidth,
                highlightRegion.LayoutBounds.Y + Position.Y,
                fadingWidth,
                highlightRegion.LayoutBounds.Height
            );
            var fadeOutRect = new Rect(
                highlightRect.Right,
                highlightRegion.LayoutBounds.Y + Position.Y,
                fadingWidth,
                highlightRegion.LayoutBounds.Height
            );

            // Brushes
            using var fadeOutBrush = CanvasHelper.CreateHorizontalFillBrush(
                control,
                [(0f, 1f), (1f, 0f)],
                (double)highlightRect.Right,
                fadingWidth
            );

            ds.FillRectangle(highlightRect, Color.FromArgb(255, 128, 128, 128));
            ds.FillRectangle(fadeOutRect, fadeOutBrush);
        }

        /// <summary>
        /// 重建当前行遮罩
        /// 仅需在布局重构 (Relayout) 时调用
        /// </summary>
        /// <param name="control"></param>
        public void RecreateCurrentLineMask(ICanvasAnimatedControl control)
        {
            CurrentLineMask?.Dispose();
            CurrentLineMask = null;

            if (CanvasTextLayout == null)
            {
                return;
            }

            CurrentLineMask = new CanvasCommandList(control);
            using var ds = CurrentLineMask.CreateDrawingSession();

            var regions = CanvasTextLayout.GetCharacterRegions(0, OriginalText.Length);
            if (regions.Length > 0)
            {
                for (int j = 0; j < regions.Length; j++)
                {
                    var region = regions[j];
                    var rect = new Rect(
                        region.LayoutBounds.X,
                        region.LayoutBounds.Y + Position.Y,
                        region.LayoutBounds.Width,
                        region.LayoutBounds.Height
                    );
                    ds.FillRectangle(rect, Colors.White);
                }
            }
        }

        public void RecreatePlaceholder(ICanvasAnimatedControl control)
        {
            PlaceholderEffect?.Dispose();
            PlaceholderEffect = null;
            PlaceholderEffect = new CanvasCommandList(control);
        }
    }
}
