using System;
using Avalonia;
using BetterLyrics.Core.Enums;
using BetterLyrics.Avalonia.Extensions;
using BetterLyrics.Core.Extensions; // Adjusted namespace

namespace BetterLyrics.Avalonia.Renderer.LyricsRenderer;

public class VerticalLyricsLineRenderer : LyricsLineRendererBase
{
    protected override Rect ApplyNonAutoWrapOffset(Rect rect, double offset)
    {
        // Vertical layout: scrolls along Y axis when not auto-wrapping
        return new Rect(rect.X, rect.Y + offset, rect.Width, rect.Height);
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

        double playedHeight = 0;

        if (LyricsWindowStatus!.LyricsEffectSettings.WordByWordEffectMode == WordByWordEffectMode.Never ||
            (LyricsWindowStatus.LyricsEffectSettings.WordByWordEffectMode == WordByWordEffectMode.Auto &&
             !Line.IsPrimaryHasRealSyllableInfo))
        {
            playedHeight = subLineRegion.Height;
        }
        else
        {
            for (var i = startCharIndex; i < startCharIndex + charCount; i++)
            {
                if (i >= Line.PrimaryRenderChars.Count) break;

                var ch = Line.PrimaryRenderChars[i];
                if (ch.IsPlayingLastFrame)
                {
                    playedHeight += ch.LayoutRect.Height * ch.GetPlayProgress(CurrentProgressMs);
                    break;
                }

                if (ch.GetPlayProgress(CurrentProgressMs) >= 1)
                    playedHeight += ch.LayoutRect.Height;
                else
                    break;
            }
        }

        return Math.Clamp((float)(playedHeight / subLineRegion.Height), 0f, 1f);
    }

    // Note: CreateGradientBrush is intentionally omitted as it was removed from 
    // the Avalonia base class (gradient stops are mapped natively in RenderLyricsRegion).
    // If you need to fix the gradient direction for vertical text, adjust the startPt/endPt 
    // in LyricsLineRendererBase.DrawSubLineRegion to be Top/Bottom instead of Left/Right.

    protected override Rect GetPlayedCharCropRect(Rect sourceCharRect, double progressPlayed)
    {
        // Vertical layout: crop height based on progress
        return new Rect(sourceCharRect.X, sourceCharRect.Y, sourceCharRect.Width, sourceCharRect.Height * progressPlayed);
    }

    protected override Rect ApplyFloatOffset(Rect rect, double floatOffset)
    {
        // Vertical layout: floats along X axis (left/right)
        return new Rect(rect.X - floatOffset, rect.Y, rect.Width, rect.Height);
    }
}