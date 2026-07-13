using System;
using System.Numerics;
using Windows.Foundation;
using BetterLyrics.Core.Enums;
using BetterLyrics.WinUI3.Extensions;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;

namespace BetterLyrics.WinUI3.Renderer.LyricsRenderer;

public class VerticalLyricsLineRenderer : LyricsLineRendererBase
{
    protected override Rect ApplyNonAutoWrapOffset(Rect rect, double offset)
    {
        return rect.AddY(offset);
    }

    protected override float CalculateRegionPlayProgress(int regionIndex)
    {
        if (Line?.PrimaryTextRegions == null || regionIndex >= Line.PrimaryTextRegions.Length) return 0f;

        var subLineRegion = Line.PrimaryTextRegions[regionIndex];
        double playedHeight = 0;

        if (LyricsWindowStatus!.LyricsEffectSettings.WordByWordEffectMode == WordByWordEffectMode.Never ||
            (LyricsWindowStatus.LyricsEffectSettings.WordByWordEffectMode == WordByWordEffectMode.Auto &&
             !Line.IsPrimaryHasRealSyllableInfo))
            playedHeight = subLineRegion.LayoutBounds.Height;
        else
            for (var i = subLineRegion.CharacterIndex;
                 i < subLineRegion.CharacterIndex + subLineRegion.CharacterCount;
                 i++)
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

        return Math.Clamp((float)(playedHeight / subLineRegion.LayoutBounds.Height), 0f, 1f);
    }

    protected override CanvasLinearGradientBrush CreateGradientBrush(ICanvasResourceCreator resourceCreator,
        CanvasGradientStop[] stops, Rect bounds)
    {
        return new CanvasLinearGradientBrush(resourceCreator, stops)
        {
            StartPoint = new Vector2((float)bounds.X, (float)bounds.Y),
            EndPoint = new Vector2((float)bounds.X, (float)(bounds.Y + bounds.Height))
        };
    }

    protected override Rect GetPlayedCharCropRect(Rect sourceCharRect, double progressPlayed)
    {
        // 竖排：裁剪高度的进度
        return new Rect(sourceCharRect.X, sourceCharRect.Y, sourceCharRect.Width,
            sourceCharRect.Height * progressPlayed);
    }

    protected override Rect ApplyFloatOffset(Rect rect, double floatOffset)
    {
        // 竖排：字沿 X 轴（左右）跳动，更生动
        // 注意：因为从右向左读，可能要加个负号决定它往左飘还是往右飘
        return rect.AddX(-floatOffset);
    }
}