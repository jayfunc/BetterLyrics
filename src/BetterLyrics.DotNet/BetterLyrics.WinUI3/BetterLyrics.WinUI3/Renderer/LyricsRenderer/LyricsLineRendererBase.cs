using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Models.Lyrics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Effects;
using System;
using Windows.Foundation;
using Windows.UI;

namespace BetterLyrics.WinUI3.Renderer.LyricsRenderer;

public abstract class LyricsLineRendererBase
{
    private const float NormalFrontGain = 1.5f;

    public bool IsPlaying { get; set; }
    public int StrokeWidth { get; set; }
    public double CurrentProgressMs { get; set; }
    public double LyricsWidth { get; set; }
    public double LyricsHeight { get; set; }
    public float HdrHighlightGain { get; set; } = 1;

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

        var hdrEnabled = HdrHighlightGain > 1 && playedOpacity > 0;
        using var hdrFrontMask = hdrEnabled && progressInRegion is > 0 and < 1
            ? CreateHdrFrontMask(resourceCreator, subLineRect, progressInRegion,
                fadeProgressInRegion * firstCharProgressInRegion, subLineRegion.CharacterCount)
            : null;
        using var hdrFill = hdrFrontMask != null || (hdrEnabled && LyricsWindowStatus.LyricsEffectSettings.IsLyricsGlowEffectEnabled)
            ? new HdrGlyphFill(region, hdrFrontMask, subLineRect)
            : null;
        var normalBoost = hdrFrontMask == null ? 0 : Math.Min(HdrHighlightGain, NormalFrontGain) - 1;

        if (hdrFill == null && !LyricsWindowStatus.LyricsEffectSettings.IsLyricsFloatAnimationEnabled &&
            !LyricsWindowStatus.LyricsEffectSettings.IsLyricsGlowEffectEnabled &&
            !LyricsWindowStatus.LyricsEffectSettings.IsLyricsScaleEffectEnabled)
        {
            ds.DrawImage(finalOutputImage);
        }
        else
        {
            var endCharIndex = subLineRegion.CharacterIndex + subLineRegion.CharacterCount;
            for (var i = subLineRegion.CharacterIndex; i < endCharIndex; i++)
                DrawSingleCharacter(ds, i, finalOutputImage, hdrFill, normalBoost, fillStops[0].Color);
        }
    }

    private void DrawSingleCharacter(CanvasDrawingSession ds, int charIndex, ICanvasImage source,
        HdrGlyphFill? hdrFill, float normalBoost, Color hdrColor)
    {
        if (LyricsWindowStatus == null || Line?.PrimaryTextLayout == null ||
            charIndex >= Line.PrimaryRenderChars.Count) return;

        var renderChar = Line.PrimaryRenderChars[charIndex];
        var (sourceCharRect, destCharRect) = GetCharacterRects(renderChar, Line,
            LyricsWindowStatus.LyricsStyleSettings.AutoWrap);

        var hdrGlowStrength = 0f;
        var playedRect = sourceCharRect;
        var glowBlur = (float)renderChar.GlowTransition.Value;
        if (glowBlur > 0)
        {
            playedRect = GetPlayedCharCropRect(sourceCharRect, renderChar.ProgressPlayed);
            renderChar.Crop.SourceRectangle = playedRect;
            renderChar.Crop.Source = source;
            renderChar.Glow.BlurAmount = glowBlur;
            var glowSource = sourceCharRect.Extend(sourceCharRect.Height);
            var glowDestination = destCharRect.Extend(destCharRect.Height);
            ds.DrawImage(renderChar.Glow, glowDestination, glowSource);
            if (hdrFill != null && LyricsWindowStatus.LyricsEffectSettings.IsLyricsGlowEffectEnabled)
            {
                hdrGlowStrength = EasingHelper.SmoothStep(Math.Clamp(glowBlur / 4, 0, 1));
                renderChar.DrawHdrHalo(ds, glowSource, glowDestination, HdrHighlightGain, hdrColor, hdrGlowStrength);
            }
        }

        if (hdrFill != null && (normalBoost > 0 || hdrGlowStrength > 0))
        {
            var peakBoost = HdrHighlightGain - 1;
            var frontBoost = normalBoost > 0 ? normalBoost + (peakBoost - normalBoost) * hdrGlowStrength : 0;
            source = hdrFill.Apply(playedRect, frontBoost, peakBoost * hdrGlowStrength * 0.78f);
        }

        ds.DrawImage(source, destCharRect, sourceCharRect);
    }

    private CanvasCommandList CreateHdrFrontMask(ICanvasResourceCreator resourceCreator, Rect regionRect,
        float progress, float fadeWidth, int characterCount)
    {
        using var brush = CreateGradientBrush(resourceCreator,
            CreateHdrFrontStops(progress, fadeWidth, characterCount), regionRect);
        var mask = new CanvasCommandList(resourceCreator);
        using (var fill = mask.CreateDrawingSession())
            fill.FillRectangle(regionRect, brush);
        return mask;
    }

    internal static CanvasGradientStop[] CreateHdrFrontStops(float progress, float fadeWidth, int characterCount)
    {
        var trail = Math.Min(0.22f, 0.9f / characterCount);
        var leadingFade = Math.Min(fadeWidth, trail * 0.25f);
        var edgeFade = EasingHelper.SmoothStep(Math.Clamp(Math.Min(progress, 1 - progress) / (trail * 0.65f), 0, 1));
        var peakAlpha = (byte)MathF.Round(255 * edgeFade);

        return
        [
            new() { Position = progress - trail, Color = Color.FromArgb(0, 255, 255, 255) },
            new() { Position = progress - trail * 0.65f, Color = Color.FromArgb((byte)(peakAlpha * 0.28f), 255, 255, 255) },
            new() { Position = progress - trail * 0.14f, Color = Color.FromArgb(peakAlpha, 255, 255, 255) },
            new() { Position = progress, Color = Color.FromArgb((byte)(peakAlpha * 0.85f), 255, 255, 255) },
            new() { Position = progress + leadingFade, Color = Color.FromArgb(0, 255, 255, 255) }
        ];
    }

    private (Rect Source, Rect Destination) GetCharacterRects(RenderLyricsChar character, RenderLyricsLine line,
        bool autoWrap)
    {
        var rect = character.LayoutRect;
        var source = new Rect(rect.X + line.PrimaryPosition.X, rect.Y + line.PrimaryPosition.Y, rect.Width, rect.Height);
        var destination = ApplyFloatOffset(source.Scale(character.ScaleTransition.Value), character.FloatTransition.Value);
        if (!autoWrap) destination = ApplyNonAutoWrapOffset(destination, line.PrimaryXOffsetTransition.Value);
        return (source, destination);
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

        var padding = blur * 3;
        ds.DrawImage(opacityEffect, destRect.Extend(padding), srcRect.Extend(padding));
    }
}
