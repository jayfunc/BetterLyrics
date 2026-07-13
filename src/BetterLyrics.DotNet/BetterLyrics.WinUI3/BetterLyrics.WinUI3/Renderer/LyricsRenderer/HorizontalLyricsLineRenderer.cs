using System;
using System.Numerics;
using Windows.Foundation;
using BetterLyrics.Core.Enums;
using BetterLyrics.WinUI3.Extensions;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;

namespace BetterLyrics.WinUI3.Renderer.LyricsRenderer;

public class HorizontalLyricsLineRenderer : LyricsLineRendererBase
{
    protected override Rect ApplyNonAutoWrapOffset(Rect rect, double offset)
    {
        return rect.AddX(offset); // 横排是不换行时沿 X 轴滚动
    }

    protected override float CalculateRegionPlayProgress(int regionIndex)
    {
        if (Line?.PrimaryTextRegions == null || regionIndex >= Line.PrimaryTextRegions.Length) return 0f;

        var subLineRegion = Line.PrimaryTextRegions[regionIndex];
        double playedWidth = 0;

        // 1. 检查是否启用了逐字卡拉OK特效 (恢复你的原版逻辑)
        if (LyricsWindowStatus!.LyricsEffectSettings.WordByWordEffectMode == WordByWordEffectMode.Never ||
            (LyricsWindowStatus.LyricsEffectSettings.WordByWordEffectMode == WordByWordEffectMode.Auto &&
             !Line.IsPrimaryHasRealSyllableInfo))
            playedWidth = subLineRegion.LayoutBounds.Width;
        else
            // 2. 逐字宽度累加计算
            for (var i = subLineRegion.CharacterIndex;
                 i < subLineRegion.CharacterIndex + subLineRegion.CharacterCount;
                 i++)
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

        return Math.Clamp((float)(playedWidth / subLineRegion.LayoutBounds.Width), 0f, 1f);
    }

    protected override CanvasLinearGradientBrush CreateGradientBrush(ICanvasResourceCreator resourceCreator,
        CanvasGradientStop[] stops, Rect bounds)
    {
        // 横排：从左向右渐变
        return new CanvasLinearGradientBrush(resourceCreator, stops)
        {
            StartPoint = new Vector2((float)bounds.X, (float)bounds.Y),
            EndPoint = new Vector2((float)(bounds.X + bounds.Width), (float)bounds.Y)
        };
    }

    protected override Rect GetPlayedCharCropRect(Rect sourceCharRect, double progressPlayed)
    {
        // 横排：只裁剪宽度的进度
        return new Rect(sourceCharRect.X, sourceCharRect.Y, sourceCharRect.Width * progressPlayed,
            sourceCharRect.Height);
    }

    protected override Rect ApplyFloatOffset(Rect rect, double floatOffset)
    {
        // 横排：字沿 Y 轴（上下）跳动
        return rect.AddY(floatOffset);
    }
}