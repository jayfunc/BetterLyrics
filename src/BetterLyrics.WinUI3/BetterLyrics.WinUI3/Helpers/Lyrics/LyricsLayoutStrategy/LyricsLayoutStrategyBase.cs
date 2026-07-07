using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Models.Lyrics;
using Microsoft.Graphics.Canvas.UI.Xaml;

namespace BetterLyrics.WinUI3.Helpers.Lyrics.LyricsLayoutStrategy;

public abstract class LyricsLayoutStrategyBase : ILyricsLayoutStrategy
{
    public abstract void MeasureAndArrange(ICanvasAnimatedControl? resourceCreator,
        IList<RenderLyricsLine>? lines,
        LyricsWindowStatus status, AppSettings appSettings, double canvasWidth, double canvasHeight,
        double lyricsWidth, double lyricsHeight);

    public abstract double? CalculateTargetScrollOffset(IList<RenderLyricsLine>? lines, int playingLineIndex);

    public abstract (int Start, int End) CalculateVisibleRange(IList<RenderLyricsLine>? lines,
        double currentScrollOffset, double lyricsOffset, double lyricsSize, double playingLineOffsetFactor);

    public abstract int FindMouseHoverLineIndex(IList<RenderLyricsLine>? lines, bool isMouseInLyricsArea,
        Point mousePosition, double currentScrollOffset, double lyricsOffset, double lyricsSize,
        double playingLineOffsetFactor);

    public abstract double CalculateActualSize(IList<RenderLyricsLine>? lines);

    public static (int Start, int End) CalculateMaxRange(IList<RenderLyricsLine>? lines)
    {
        if (lines == null || lines.Count == 0) return (-1, -1);
        return (0, lines.Count - 1);
    }

    public static void CalculateLanes(IList<RenderLyricsLine>? lines, int toleranceMs = 50)
    {
        if (lines == null) return;
        var lanesEndMs = new List<int> { 0 };

        foreach (var line in lines)
        {
            var start = line.StartMs;
            var end = line.EndMs;

            var assignedLane = -1;
            for (var i = 0; i < lanesEndMs.Count; i++)
                if (lanesEndMs[i] <= start + toleranceMs)
                {
                    assignedLane = i;
                    break;
                }

            if (assignedLane == -1)
            {
                assignedLane = lanesEndMs.Count;
                lanesEndMs.Add(0);
            }

            lanesEndMs[assignedLane] = end ?? 0;
            line.LaneIndex = assignedLane;
        }
    }

    public static void CalculateAlignments(IList<RenderLyricsLine>? lines)
    {
        if (lines == null || lines.Count == 0) return;

        var uniqueAgents = lines
            .Where(l => !string.IsNullOrEmpty(l.AgentId))
            .Select(l => l.AgentId)
            .Distinct()
            .ToList();

        Dictionary<string, TextAlignmentType> alignmentMap = new();

        for (var i = 0; i < uniqueAgents.Count; i++)
        {
            var agent = uniqueAgents[i];
            if (agent == "v1000" || agent.Contains("group", StringComparison.OrdinalIgnoreCase))
            {
                alignmentMap[agent] = TextAlignmentType.Center;
                continue;
            }

            if (i == 0) alignmentMap[agent] = TextAlignmentType.Left;
            else if (i == 1) alignmentMap[agent] = TextAlignmentType.Right;
            else alignmentMap[agent] = i % 2 == 0 ? TextAlignmentType.Left : TextAlignmentType.Right;
        }

        foreach (var line in lines)
            if (string.IsNullOrEmpty(line.AgentId))
                line.HorizontalAlignmentType = null;
            else if (alignmentMap.TryGetValue(line.AgentId, out var alignment))
                line.HorizontalAlignmentType = alignment;
    }
}