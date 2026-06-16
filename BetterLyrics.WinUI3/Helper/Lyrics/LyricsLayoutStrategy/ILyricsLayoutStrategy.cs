using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Models.Settings;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System.Collections.Generic;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Helper.Lyrics.LyricsLayoutStrategy
{
    public interface ILyricsLayoutStrategy
    {
        void MeasureAndArrange(
           ICanvasAnimatedControl resourceCreator,
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
}
