using System.Collections.Generic;
using Windows.Foundation;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Models.Lyrics;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace BetterLyrics.WinUI3.Helpers.Lyrics.LyricsLayoutStrategy;

public interface ILyricsLayoutStrategy
{
    void MeasureAndArrange(
        ICanvasAnimatedControl? resourceCreator,
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