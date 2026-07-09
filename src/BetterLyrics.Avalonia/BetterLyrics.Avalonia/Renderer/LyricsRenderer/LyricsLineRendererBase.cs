using System;
using Avalonia;
using Avalonia.Media;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Avalonia.Extensions;
using BetterLyrics.Avalonia.Models.Lyrics;
using BetterLyrics.Core.Extensions;

namespace BetterLyrics.Avalonia.Renderer.LyricsRenderer;

public abstract class LyricsLineRendererBase
{
    public bool IsPlaying { get; set; }
    public int StrokeWidth { get; set; }
    public double CurrentProgressMs { get; set; }
    public double LyricsWidth { get; set; }
    public double LyricsHeight { get; set; }

    public RenderLyricsLine? Line { get; set; }
    public LyricsWindowStatus? LyricsWindowStatus { get; set; }

    public void Draw(DrawingContext context)
    {
        DrawTertiaryText(context);
        DrawPrimaryText(context);
        DrawSecondaryText(context);
    }

    protected abstract Rect ApplyNonAutoWrapOffset(Rect rect, double offset);

    protected abstract float CalculateRegionPlayProgress(int regionIndex);

    // Note: CreateGradientBrush is removed. The LinearGradientBrush is now built natively 
    // inside RenderLyricsRegion.Render() when utilizing Avalonia.

    protected abstract Rect GetPlayedCharCropRect(Rect sourceCharRect, double progressPlayed);

    protected abstract Rect ApplyFloatOffset(Rect rect, double floatOffset);

    private void DrawTertiaryText(DrawingContext context)
    {
        if (LyricsWindowStatus == null || Line == null || Line.TertiaryTextLayout == null) return;
        var opacity = Line.TertiaryOpacityTransition.Value;
        var blur = Line.BlurAmountTransition.Value;
        if (double.IsNaN(opacity) || opacity <= 0) return;

        // Note: Avalonia TextLayout size requires manual extension calculation
        var bounds =
            new Rect(0, 0, Line.TertiaryTextLayout.Width, Line.TertiaryTextLayout.Height).Extend(StrokeWidth / 2f);
        var srcRect = new Rect(bounds.X + Line.TertiaryPosition.X, bounds.Y + Line.TertiaryPosition.Y, bounds.Width,
            bounds.Height);
        var destRect = srcRect;

        if (!LyricsWindowStatus.LyricsStyleSettings.AutoWrap)
            destRect = ApplyNonAutoWrapOffset(destRect, Line.TertiaryXOffsetTransition.Value);

        DrawImageWithEffects(context, Line.CachedFill, Line.CachedStroke, srcRect, destRect, blur, opacity);
    }

    private void DrawSecondaryText(DrawingContext context)
    {
        if (LyricsWindowStatus == null || Line == null || Line.SecondaryTextLayout == null) return;
        var opacity = Line.SecondaryOpacityTransition.Value;
        var blur = Line.BlurAmountTransition.Value;
        if (double.IsNaN(opacity) || opacity <= 0) return;

        var bounds =
            new Rect(0, 0, Line.SecondaryTextLayout.Width, Line.SecondaryTextLayout.Height).Extend(StrokeWidth / 2f);
        var srcRect = new Rect(bounds.X + Line.SecondaryPosition.X, bounds.Y + Line.SecondaryPosition.Y, bounds.Width,
            bounds.Height);
        var destRect = srcRect;

        if (!LyricsWindowStatus.LyricsStyleSettings.AutoWrap)
            destRect = ApplyNonAutoWrapOffset(destRect, Line.SecondaryXOffsetTransition.Value);

        DrawImageWithEffects(context, Line.CachedFill, Line.CachedStroke, srcRect, destRect, blur, opacity);
    }

    private void DrawPrimaryText(DrawingContext context)
    {
        if (LyricsWindowStatus == null || Line?.PrimaryTextLayout == null ||
            Line.PrimaryTextRegions == null) return;

        // TODO
        //var bounds =
        //    new Rect(0, 0, Line.PrimaryTextLayout.Width, Line.PrimaryTextLayout.Height).Extend(StrokeWidth / 2f);
        var bounds =
            new Rect(0, 0, Line.PrimaryTextLayout.Width, Line.PrimaryTextLayout.Height);
        var srcRect = new Rect(bounds.X + Line.PrimaryPosition.X, bounds.Y + Line.PrimaryPosition.Y, bounds.Width,
            bounds.Height);
        var destRect = srcRect;

        if (!LyricsWindowStatus.LyricsStyleSettings.AutoWrap)
            destRect = ApplyNonAutoWrapOffset(destRect, Line.PrimaryXOffsetTransition.Value);

        if (IsPlaying)
        {
            for (var i = 0; i < Line.PrimaryTextRegions.Count; i++)
                DrawSubLineRegion(context, i);
        }
        else
        {
            var opacity = MathF.Max((float)Line.PlayedPrimaryOpacityTransition.Value,
                (float)Line.UnplayedPrimaryOpacityTransition.Value);
            if (double.IsNaN(opacity)) return;

            DrawImageWithEffects(context, Line.CachedFill, Line.CachedStroke, srcRect, destRect,
                Line.BlurAmountTransition.Value, opacity);
        }
    }

    private void DrawSubLineRegion(DrawingContext context, int regionIndex)
    {
        if (LyricsWindowStatus == null) return;
        if (Line == null) return;
        if (Line.PrimaryTextRegions == null) return;

        if (Line.RenderLyricsRegions == null || regionIndex >= Line.RenderLyricsRegions.Length) return;

        // Avalonia HitTestTextRange returns standard Rects instead of structured regions
        var subLineRegion = Line.PrimaryTextRegions[regionIndex];

        // Calculate original start and count (Assumes one region per line/word based on how regions are split)
        // Since Avalonia HitTest doesn't natively expose "CharacterCount" in a simple rect, we infer limits from RenderChars mapping
        int startCharIndex = -1;
        int charCount = 0;

        for (int i = 0; i < Line.PrimaryRenderChars.Count; i++)
        {
            // Simple bound check to map the rect back to the characters inside it
            if (subLineRegion.Contains(new Point(Line.PrimaryRenderChars[i].LayoutRect.Center.X,
                    Line.PrimaryRenderChars[i].LayoutRect.Center.Y)))
            {
                if (startCharIndex == -1) startCharIndex = i;
                charCount++;
            }
        }

        if (startCharIndex == -1) return;

        var playedOpacity = Line.PlayedPrimaryOpacityTransition.Value;
        var unplayedOpacity = Line.UnplayedPrimaryOpacityTransition.Value;

        var playedFillColor = Line.PlayedFillColorTransition.Value;
        var unplayedFillColor = Line.UnplayedFillColorTransition.Value;
        var playedStrokeColor = Line.PlayedStrokeColorTransition.Value;
        var unplayedStrokeColor = Line.UnplayedStrokeColorTransition.Value;

        var subLineLayoutBounds = subLineRegion.Extend(StrokeWidth, StrokeWidth / 2f);
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
        {
            playedWidth = subLineRegion.Width;
        }
        else
        {
            for (var i = startCharIndex; i < startCharIndex + charCount; i++)
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
        }

        var progressInRegion = CalculateRegionPlayProgress(regionIndex);
        var fadeProgressInRegion = 1f / charCount * 0.5f;

        if (startCharIndex >= Line.PrimaryRenderChars.Count) return;
        var firstCharProgressInRegion =
            Math.Clamp(
                (float)Line.PrimaryRenderChars[startCharIndex].GetPlayProgress(CurrentProgressMs), 0f, 1f);

        // Retrieve the RenderLyricsRegion cache from Step 4
        var region = Line.RenderLyricsRegions[regionIndex];

        // Populate stops directly on the region
        var fillStops = region.FillStops;
        fillStops[0].Offset = 0;
        fillStops[0].Color = ColorExtensions.FromAppColor(playedFillColor.WithAlpha((byte)(255 * playedOpacity)));
        fillStops[1].Offset = progressInRegion;
        fillStops[1].Color = ColorExtensions.FromAppColor(playedFillColor.WithAlpha((byte)(255 * playedOpacity)));
        fillStops[2].Offset = progressInRegion + fadeProgressInRegion * firstCharProgressInRegion;
        fillStops[2].Color = ColorExtensions.FromAppColor(unplayedFillColor.WithAlpha((byte)(255 * unplayedOpacity)));
        fillStops[3].Offset = 1 + fadeProgressInRegion;
        fillStops[3].Color = ColorExtensions.FromAppColor(unplayedFillColor.WithAlpha((byte)(255 * unplayedOpacity)));

        var strokeStops = region.StrokeStops;
        strokeStops[0].Offset = 0;
        strokeStops[0].Color = ColorExtensions.FromAppColor(playedStrokeColor.WithAlpha((byte)(255 * playedOpacity)));
        strokeStops[1].Offset = progressInRegion;
        strokeStops[1].Color = ColorExtensions.FromAppColor(playedStrokeColor.WithAlpha((byte)(255 * playedOpacity)));
        strokeStops[2].Offset = progressInRegion + fadeProgressInRegion * firstCharProgressInRegion;
        strokeStops[2].Color =
            ColorExtensions.FromAppColor(unplayedStrokeColor.WithAlpha((byte)(255 * unplayedOpacity)));
        strokeStops[3].Offset = 1 + fadeProgressInRegion;
        strokeStops[3].Color =
            ColorExtensions.FromAppColor(unplayedStrokeColor.WithAlpha((byte)(255 * unplayedOpacity)));

        // Coordinate space mapping for LinearGradientBrush based on LyricsLayoutOrientation
        Point startPoint, endPoint;
        if (LyricsWindowStatus.LyricsStyleSettings.LyricsLayoutOrientation == LyricsLayoutOrientation.Vertical)
        {
            startPoint = new Point(subLineRect.Right, subLineRect.Top);
            endPoint = new Point(subLineRect.Left, subLineRect.Top);
        }
        else
        {
            startPoint = new Point(subLineRect.Left, subLineRect.Top);
            endPoint = new Point(subLineRect.Right, subLineRect.Top);
        }

        if (!LyricsWindowStatus.LyricsEffectSettings.IsLyricsFloatAnimationEnabled &&
            !LyricsWindowStatus.LyricsEffectSettings.IsLyricsGlowEffectEnabled &&
            !LyricsWindowStatus.LyricsEffectSettings.IsLyricsScaleEffectEnabled)
        {
            // Utilize the new Avalonia Render method built in Step 4
            region.Render(context, subLineRect, startPoint, endPoint);
        }
        else
        {
            var endCharIndex = startCharIndex + charCount;
            for (var i = startCharIndex; i < endCharIndex; i++)
                DrawSingleCharacter(context, i, region, startPoint, endPoint, subLineRect);
        }
    }

    private void DrawSingleCharacter(DrawingContext context, int charIndex, RenderLyricsRegion region, Point startPt,
        Point endPt, Rect subLineRect)
    {
        if (LyricsWindowStatus == null || Line?.PrimaryTextLayout == null ||
            charIndex >= Line.PrimaryRenderChars.Count) return;

        var renderChar = Line.PrimaryRenderChars[charIndex];
        var rect = renderChar.LayoutRect;
        var sourceCharRect = new Rect(rect.X + Line.PrimaryPosition.X, rect.Y + Line.PrimaryPosition.Y, rect.Width,
            rect.Height);

        var destCharRect = ApplyFloatOffset(sourceCharRect.Scale(renderChar.ScaleTransition.Value),
            renderChar.FloatTransition.Value);

        if (!LyricsWindowStatus.LyricsStyleSettings.AutoWrap)
            destCharRect = ApplyNonAutoWrapOffset(destCharRect, Line.PrimaryXOffsetTransition.Value);

        // Apply a translation to simulate the floating/scaling offset
        double offsetX = destCharRect.X - sourceCharRect.X;
        double offsetY = destCharRect.Y - sourceCharRect.Y;

        // Pushing translation ensures the region's drawing operations offset along with the character
        using (context.PushTransform(Matrix.CreateTranslation(offsetX, offsetY)))
        {
            // Note: Since Avalonia does not extract individual glyph pixels for glow as easily as Win2D's CropEffect, 
            // a custom ICustomDrawOperation utilizing Skia SKImageFilter.CreateBlur mapping to a specific char clip 
            // region would be required here for the "Glow" implementation if strictly necessary. 
            // For standard rendering, we clip to the character bounds and invoke the region render.

            using (context.PushClip(sourceCharRect))
            {
                // Glow fallback (if required by specs, could render the region twice with a slight scale/opacity push)

                // Base character render
                region.Render(context, subLineRect, startPt, endPt);
            }
        }
    }

    private static void DrawImageWithEffects(DrawingContext context, DrawingGroup? fillMask, DrawingGroup? strokeMask,
        Rect srcRect, Rect destRect, double blur, double opacity)
    {
        if (fillMask == null) return;

        var opacityLevel = Math.Clamp(opacity, 0, 1);

        using (context.PushOpacity(opacityLevel))
        {
            // Note: Native Avalonia DrawingContext does not support arbitrary blur nodes like GaussianBlurEffect 
            // without dropping into a Skia SKCanvas CustomDrawOperation. 
            // If blurred text is a strict requirement for secondary layers, an ICustomDrawOperation should wrap this.

            double offsetX = destRect.X - srcRect.X;
            double offsetY = destRect.Y - srcRect.Y;

            using (context.PushTransform(Matrix.CreateTranslation(offsetX, offsetY)))
            using (context.PushClip(srcRect))
            {
                var fallbackBrush = Brushes.White; // Static unplayed color fallback

                if (strokeMask != null)
                {
                    using (context.PushOpacityMask(new DrawingBrush(strokeMask) { Stretch = Stretch.None }, srcRect))
                    {
                        context.DrawRectangle(fallbackBrush, null, srcRect);
                    }
                }

                using (context.PushOpacityMask(new DrawingBrush(fillMask) { Stretch = Stretch.None }, srcRect))
                {
                    context.DrawRectangle(fallbackBrush, null, srcRect);
                }
            }
        }
    }
}