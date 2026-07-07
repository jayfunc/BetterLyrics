using Avalonia;
using BetterLyrics.Avalonia.Models.Lyrics;
using BetterLyrics.Core.Models.Settings;
using System.Collections.Generic;

namespace BetterLyrics.Avalonia.Helpers.Lyrics.LyricsLayoutStrategy;

public interface ILyricsLayoutStrategy
{
    // Removed ICanvasAnimatedControl dependency
    void MeasureAndArrange(
        IList<RenderLyricsLine>? lines,
        LyricsWindowStatus status,
        AppSettings appSettings,
        double canvasWidth,
        double canvasHeight,
        double lyricsWidth,
        double lyricsHeight);

    double? CalculateTargetScrollOffset(IList<RenderLyricsLine>? lines, int playingLineIndex);

    (int Start, int End) CalculateVisibleRange(
        IList<RenderLyricsLine>? lines,
        double currentScrollOffset,
        double lyricsOffset,
        double lyricsSize,
        double playingLineOffsetFactor);

    int FindMouseHoverLineIndex(
        IList<RenderLyricsLine>? lines,
        bool isMouseInLyricsArea,
        Point mousePosition,
        double currentScrollOffset,
        double lyricsOffset,
        double lyricsSize,
        double playingLineOffsetFactor);

    double CalculateActualSize(IList<RenderLyricsLine>? lines);
}