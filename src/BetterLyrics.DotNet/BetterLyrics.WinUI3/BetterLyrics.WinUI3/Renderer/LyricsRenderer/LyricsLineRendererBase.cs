using System;
using Windows.Foundation;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Models.Lyrics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;

namespace BetterLyrics.WinUI3.Renderer.LyricsRenderer;

public abstract class LyricsLineRendererBase
{
    public bool IsPlaying { get; set; }
    public int StrokeWidth { get; set; }
    public double CurrentProgressMs { get; set; }
    public double LyricsWidth { get; set; }
    public double LyricsHeight { get; set; }

    public RenderLyricsLine? Line { get; set; }
    public LyricsWindowStatus? LyricsWindowStatus { get; set; }

    public void Draw(ICanvasResourceCreator resourceCreator, CanvasDrawingSession ds)
    {
        DrawTertiaryText(ds);
        DrawPrimaryText(resourceCreator, ds);
        DrawSecondaryText(ds);
    }

    protected abstract Rect ApplyNonAutoWrapOffset(Rect rect, double offset);

    protected abstract float CalculateRegionPlayProgress(int regionIndex);

    protected abstract CanvasLinearGradientBrush CreateGradientBrush(ICanvasResourceCreator resourceCreator,
        CanvasGradientStop[] stops, Rect bounds);

    protected abstract Rect GetPlayedCharCropRect(Rect sourceCharRect, double progressPlayed);

    protected abstract Rect ApplyFloatOffset(Rect rect, double floatOffset);

    private void DrawTertiaryText(CanvasDrawingSession ds)
    {
        if (LyricsWindowStatus == null || Line == null || Line.TertiaryTextLayout == null) return;
        var opacity = Line.TertiaryOpacityTransition.Value;
        var blur = Line.BlurAmountTransition.Value;
        if (double.IsNaN(opacity) || opacity <= 0) return;

        var bounds = Line.TertiaryTextLayout.LayoutBounds.Extend(StrokeWidth / 2f);
        var srcRect = new Rect(bounds.X + Line.TertiaryPosition.X, bounds.Y + Line.TertiaryPosition.Y, bounds.Width,
            bounds.Height);
        var destRect = srcRect;

        if (!LyricsWindowStatus.LyricsStyleSettings.AutoWrap)
            destRect = ApplyNonAutoWrapOffset(destRect, Line.TertiaryXOffsetTransition.Value);

        DrawImageWithEffects(ds, Line.UnplayedComposite, srcRect, destRect, blur, opacity);
    }

    private void DrawSecondaryText(CanvasDrawingSession ds)
    {
        if (LyricsWindowStatus == null || Line == null || Line.SecondaryTextLayout == null) return;
        var opacity = Line.SecondaryOpacityTransition.Value;
        var blur = Line.BlurAmountTransition.Value;
        if (double.IsNaN(opacity) || opacity <= 0) return;

        var bounds = Line.SecondaryTextLayout.LayoutBounds.Extend(StrokeWidth / 2f);
        var srcRect = new Rect(bounds.X + Line.SecondaryPosition.X, bounds.Y + Line.SecondaryPosition.Y,
            bounds.Width, bounds.Height);
        var destRect = srcRect;

        if (!LyricsWindowStatus.LyricsStyleSettings.AutoWrap)
            destRect = ApplyNonAutoWrapOffset(destRect, Line.SecondaryXOffsetTransition.Value);

        DrawImageWithEffects(ds, Line.UnplayedComposite, srcRect, destRect, blur, opacity);
    }

    private void DrawPrimaryText(ICanvasResourceCreator resourceCreator, CanvasDrawingSession ds)
    {
        if (LyricsWindowStatus == null || Line?.PrimaryTextLayout == null ||
            Line.PrimaryTextRegions == null) return;

        var bounds = Line.PrimaryTextLayout.LayoutBounds.Extend(StrokeWidth / 2f);
        var srcRect = new Rect(bounds.X + Line.PrimaryPosition.X, bounds.Y + Line.PrimaryPosition.Y, bounds.Width,
            bounds.Height);
        var destRect = srcRect;

        if (!LyricsWindowStatus.LyricsStyleSettings.AutoWrap)
            destRect = ApplyNonAutoWrapOffset(destRect, Line.PrimaryXOffsetTransition.Value);

        if (IsPlaying)
        {
            for (var i = 0; i < Line.PrimaryTextRegions.Length; i++)
                DrawSubLineRegion(resourceCreator, ds, i);
        }
        else
        {
            var opacity = MathF.Max((float)Line.PlayedPrimaryOpacityTransition.Value,
                (float)Line.UnplayedPrimaryOpacityTransition.Value);
            if (double.IsNaN(opacity)) return;
            DrawImageWithEffects(ds, Line.UnplayedComposite, srcRect, destRect, Line.BlurAmountTransition.Value,
                opacity);
        }
    }

    private void DrawSubLineRegion(ICanvasResourceCreator resourceCreator, CanvasDrawingSession ds, int regionIndex)
    {
        if (LyricsWindowStatus == null) return;
        if (Line == null) return;
        if (Line.PrimaryTextRegions == null) return;

        if (Line.RenderLyricsRegions == null || regionIndex >= Line.RenderLyricsRegions.Length) return;

        var subLineRegion = Line.PrimaryTextRegions[regionIndex];

        var playedOpacity = Line.PlayedPrimaryOpacityTransition.Value;
        var unplayedOpacity = Line.UnplayedPrimaryOpacityTransition.Value;

        var playedFillColor = Line.PlayedFillColorTransition.Value;
        var unplayedFillColor = Line.UnplayedFillColorTransition.Value;
        var playedStrokeColor = Line.PlayedStrokeColorTransition.Value;
        var unplayedStrokeColor = Line.UnplayedStrokeColorTransition.Value;

        var subLineLayoutBounds = subLineRegion.LayoutBounds.Extend(StrokeWidth, StrokeWidth / 2f);
        Rect subLineRect = new(
            subLineLayoutBounds.X + Line.PrimaryPosition.X,
            subLineLayoutBounds.Y + Line.PrimaryPosition.Y,
            subLineLayoutBounds.Width,
            subLineLayoutBounds.Height
        );

        double playedWidth = 0;
        if (LyricsWindowStatus.LyricsEffectSettings.WordByWordEffectMode == WordByWordEffectMode.Never ||
            (LyricsWindowStatus.LyricsEffectSettings.WordByWordEffectMode == WordByWordEffectMode.Auto &&
             !Line.IsPrimaryHasRealSyllableInfo))
            playedWidth = subLineRegion.LayoutBounds.Width;
        else
            for (var i = subLineRegion.CharacterIndex;
                 i < subLineRegion.CharacterIndex + subLineRegion.CharacterCount;
                 i++)
            {
                if (i >= Line.PrimaryRenderChars.Count) return;
                var ch = Line.PrimaryRenderChars[i];
                if (ch.IsPlayingLastFrame)
                {
                    playedWidth += ch.LayoutRect.Width * ch.GetPlayProgress(CurrentProgressMs);
                    break;
                }

                if (ch.GetPlayProgress(CurrentProgressMs) >= 1)
                    playedWidth += ch.LayoutRect.Width;
                else
                    break;
            }

        var progressInRegion = CalculateRegionPlayProgress(regionIndex);
        var fadeProgressInRegion = 1f / subLineRegion.CharacterCount * 0.5f;

        if (subLineRegion.CharacterIndex >= Line.PrimaryRenderChars.Count) return;
        var firstCharProgressInRegion =
            Math.Clamp(
                (float)Line.PrimaryRenderChars[subLineRegion.CharacterIndex].GetPlayProgress(CurrentProgressMs), 0f,
                1f);

        // RenderLyricsRegion 缓存
        var region = Line.RenderLyricsRegions[regionIndex];

        var fillStops = region.FillStops;
        fillStops[0].Position = 0;
        fillStops[0].Color = ColorExtensions.FromAppColor(playedFillColor.WithAlpha((byte)(255 * playedOpacity)));
        fillStops[1].Position = progressInRegion;
        fillStops[1].Color = ColorExtensions.FromAppColor(playedFillColor.WithAlpha((byte)(255 * playedOpacity)));
        fillStops[2].Position = progressInRegion + fadeProgressInRegion * firstCharProgressInRegion;
        fillStops[2].Color =
            ColorExtensions.FromAppColor(unplayedFillColor.WithAlpha((byte)(255 * unplayedOpacity)));
        fillStops[3].Position = 1 + fadeProgressInRegion;
        fillStops[3].Color =
            ColorExtensions.FromAppColor(unplayedFillColor.WithAlpha((byte)(255 * unplayedOpacity)));

        var strokeStops = region.StrokeStops;
        strokeStops[0].Position = 0;
        strokeStops[0].Color =
            ColorExtensions.FromAppColor(playedStrokeColor.WithAlpha((byte)(255 * playedOpacity)));
        strokeStops[1].Position = progressInRegion;
        strokeStops[1].Color =
            ColorExtensions.FromAppColor(playedStrokeColor.WithAlpha((byte)(255 * playedOpacity)));
        strokeStops[2].Position = progressInRegion + fadeProgressInRegion * firstCharProgressInRegion;
        strokeStops[2].Color =
            ColorExtensions.FromAppColor(unplayedStrokeColor.WithAlpha((byte)(255 * unplayedOpacity)));
        strokeStops[3].Position = 1 + fadeProgressInRegion;
        strokeStops[3].Color =
            ColorExtensions.FromAppColor(unplayedStrokeColor.WithAlpha((byte)(255 * unplayedOpacity)));

        using var fillGradientBrush = CreateGradientBrush(resourceCreator, fillStops, subLineRect);
        using var fillGradientLayer = new CanvasCommandList(resourceCreator);
        using (var gds = fillGradientLayer.CreateDrawingSession())
        {
            gds.FillRectangle(subLineRect, fillGradientBrush);
        }

        region.FinalFillEffect.Source = fillGradientLayer;
        ICanvasImage finalOutputImage = region.FinalFillEffect;

        var hasStroke = Line.CachedStroke != null && region.FinalStrokeEffect != null &&
                        region.CombinedEffect != null;

        using var strokeGradientBrush =
            hasStroke ? CreateGradientBrush(resourceCreator, strokeStops, subLineRect) : null;
        using var strokeGradientLayer = hasStroke ? new CanvasCommandList(resourceCreator) : null;

        if (hasStroke)
        {
            using (var gds = strokeGradientLayer!.CreateDrawingSession())
            {
                gds.FillRectangle(subLineRect, strokeGradientBrush);
            }

            region.FinalStrokeEffect!.Source = strokeGradientLayer;
            finalOutputImage = region.CombinedEffect!;
        }

        if (!LyricsWindowStatus.LyricsEffectSettings.IsLyricsFloatAnimationEnabled &&
            !LyricsWindowStatus.LyricsEffectSettings.IsLyricsGlowEffectEnabled &&
            !LyricsWindowStatus.LyricsEffectSettings.IsLyricsScaleEffectEnabled)
        {
            ds.DrawImage(finalOutputImage);
        }
        else
        {
            var endCharIndex = subLineRegion.CharacterIndex + subLineRegion.CharacterCount;
            for (var i = subLineRegion.CharacterIndex; i < endCharIndex; i++)
                DrawSingleCharacter(ds, i, finalOutputImage);
        }
    }

    private void DrawSingleCharacter(CanvasDrawingSession ds, int charIndex, ICanvasImage source)
    {
        if (LyricsWindowStatus == null || Line?.PrimaryTextLayout == null ||
            charIndex >= Line.PrimaryRenderChars.Count) return;

        var renderChar = Line.PrimaryRenderChars[charIndex];
        var rect = renderChar.LayoutRect;
        var sourceCharRect = new Rect(rect.X + Line.PrimaryPosition.X, rect.Y + Line.PrimaryPosition.Y, rect.Width,
            rect.Height);

        // 应用浮动偏移
        var destCharRect = ApplyFloatOffset(sourceCharRect.Scale(renderChar.ScaleTransition.Value),
            renderChar.FloatTransition.Value);

        if (!LyricsWindowStatus.LyricsStyleSettings.AutoWrap)
            destCharRect = ApplyNonAutoWrapOffset(destCharRect, Line.PrimaryXOffsetTransition.Value);

        // 发光特效
        if (renderChar.GlowTransition.Value > 0)
        {
            // 使用抽象方法裁剪
            renderChar.Crop.SourceRectangle = GetPlayedCharCropRect(sourceCharRect, renderChar.ProgressPlayed);
            renderChar.Crop.Source = source;
            renderChar.Glow.BlurAmount = (float)renderChar.GlowTransition.Value;
            ds.DrawImage(renderChar.Glow, destCharRect.Extend(destCharRect.Height),
                sourceCharRect.Extend(sourceCharRect.Height));
        }

        ds.DrawImage(source, destCharRect, sourceCharRect);
    }

    private static void DrawImageWithEffects(CanvasDrawingSession ds, ICanvasImage? source, Rect srcRect,
        Rect destRect, double blur, double opacity)
    {
        if (source == null) return;

        using var cropEffect = new CropEffect
            { Source = source, BorderMode = EffectBorderMode.Hard, SourceRectangle = srcRect };
        using var blurEffect = new GaussianBlurEffect
            { BlurAmount = (float)blur, Source = cropEffect, BorderMode = EffectBorderMode.Soft };
        using var opacityEffect = new OpacityEffect { Source = blurEffect, Opacity = (float)opacity };
        ds.DrawImage(opacityEffect, destRect, srcRect);
    }
}