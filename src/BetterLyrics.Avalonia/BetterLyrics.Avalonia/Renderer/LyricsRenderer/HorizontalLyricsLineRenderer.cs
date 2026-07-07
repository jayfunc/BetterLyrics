using System;
using Avalonia;
using BetterLyrics.Core.Enums;
using BetterLyrics.Avalonia.Extensions;
using BetterLyrics.Core.Extensions;

namespace BetterLyrics.Avalonia.Renderer.LyricsRenderer;

public class HorizontalLyricsLineRenderer : LyricsLineRendererBase
{
    protected override Rect ApplyNonAutoWrapOffset(Rect rect, double offset)
    {
        // Horizontal layout: scrolls along X axis when not auto-wrapping
        return new Rect(rect.X + offset, rect.Y, rect.Width, rect.Height);
    }

    protected override float CalculateRegionPlayProgress(int regionIndex)
    {
        if (Line?.PrimaryTextRegions == null || regionIndex >= Line.PrimaryTextRegions.Count) return 0f;

        var subLineRegion = Line.PrimaryTextRegions[regionIndex];

        // Map the region rect back to the characters inside it
        int startCharIndex = -1;
        int charCount = 0;

        for (int i = 0; i < Line.PrimaryRenderChars.Count; i++)
        {
            if (subLineRegion.Contains(Line.PrimaryRenderChars[i].LayoutRect.Center.ToPoint()))
            {
                if (startCharIndex == -1) startCharIndex = i;
                charCount++;
            }
        }

        if (startCharIndex == -1) return 0f;

        double playedWidth = 0;

        if (LyricsWindowStatus!.LyricsEffectSettings.WordByWordEffectMode == WordByWordEffectMode.Never ||
            (LyricsWindowStatus.LyricsEffectSettings.WordByWordEffectMode == WordByWordEffectMode.Auto &&
             !Line.IsPrimaryHasRealSyllableInfo))
        {
            playedWidth = subLineRegion.Width;
        }
        else
        {
            for (var i = startCharIndex; i < startCharIndex + charCount; i++)
            {
                if (i >= Line.PrimaryRenderChars.Count) break;

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

        return Math.Clamp((float)(playedWidth / subLineRegion.Width), 0f, 1f);
    }

    // Note: CreateGradientBrush is intentionally omitted as it was removed from 
    // the Avalonia base class (gradient stops are mapped natively in RenderLyricsRegion).

    protected override Rect GetPlayedCharCropRect(Rect sourceCharRect, double progressPlayed)
    {
        // Horizontal layout: crop width based on progress
        return new Rect(sourceCharRect.X, sourceCharRect.Y, sourceCharRect.Width * progressPlayed,
            sourceCharRect.Height);
    }

    protected override Rect ApplyFloatOffset(Rect rect, double floatOffset)
    {
        // Horizontal layout: floats along Y axis
        return new Rect(rect.X, rect.Y + floatOffset, rect.Width, rect.Height);
    }
}