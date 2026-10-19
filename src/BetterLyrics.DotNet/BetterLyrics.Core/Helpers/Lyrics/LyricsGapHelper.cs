using System;
using System.Collections.Generic;

namespace BetterLyrics.Core.Helpers.Lyrics;

/// <summary>
/// 间隙检测所需的最小歌词行信息，索引必须与歌词行列表保持一致
/// </summary>
/// <param name="StartMs">行开始时间</param>
/// <param name="EndMs">行结束时间，未知时为 null</param>
/// <param name="IsEmpty">是否为空行（空行本身即代表一段没有歌词的间隙）</param>
public readonly record struct LyricsGapLine(int StartMs, int? EndMs, bool IsEmpty);

/// <summary>
/// 一段没有歌词的间隙（前奏、行与行之间、尾奏）
/// </summary>
/// <param name="StartMs">间隙开始时间</param>
/// <param name="EndMs">间隙结束时间</param>
/// <param name="MarkerLineIndex">标记该间隙的空行索引，没有空行时为 -1</param>
/// <param name="PreviousLineIndex">间隙前最后一句歌词的索引，前奏时为 -1</param>
/// <param name="NextLineIndex">间隙后第一句歌词的索引，尾奏时为 -1</param>
public readonly record struct LyricsGapSegment(
    int StartMs,
    int EndMs,
    int MarkerLineIndex,
    int PreviousLineIndex,
    int NextLineIndex)
{
    public int DurationMs => Math.Max(EndMs - StartMs, 0);
}

public static class LyricsGapHelper
{
    /// <summary>
    /// 计算所有时长不小于 <paramref name="minimumDurationMs" /> 的无歌词间隙
    /// </summary>
    /// <param name="lines">按时间排序的歌词行</param>
    /// <param name="songDurationMs">歌曲总时长，提供后才会识别尾奏间隙</param>
    /// <param name="minimumDurationMs">最短间隙时长，用于过滤短促的行间空白</param>
    public static List<LyricsGapSegment> FindGaps(
        IReadOnlyList<LyricsGapLine>? lines,
        double songDurationMs = 0,
        int minimumDurationMs = 0)
    {
        var gaps = new List<LyricsGapSegment>();
        if (lines == null || lines.Count == 0) return gaps;

        // 空行不占用时间轴，因此它们天然会落入下方的补集里
        var covered = new List<(int Start, int End)>(lines.Count);
        foreach (var line in lines)
        {
            if (line.IsEmpty) continue;

            var start = Math.Max(line.StartMs, 0);
            var end = line.EndMs is { } endMs && endMs > start ? endMs : start;
            if (end > start) covered.Add((start, end));
        }

        covered.Sort(static (a, b) => a.Start.CompareTo(b.Start));

        // 合并重叠区间，避免重唱 / 背景人声把时间轴切成碎片
        var merged = new List<(int Start, int End)>(covered.Count);
        foreach (var interval in covered)
        {
            if (merged.Count > 0 && interval.Start <= merged[^1].End)
            {
                if (interval.End > merged[^1].End) merged[^1] = (merged[^1].Start, interval.End);
            }
            else
            {
                merged.Add(interval);
            }
        }

        var cursor = 0;
        foreach (var interval in merged)
        {
            if (interval.Start > cursor) AddGap(gaps, lines, cursor, interval.Start, minimumDurationMs);
            if (interval.End > cursor) cursor = interval.End;
        }

        // 尾奏：只有知道歌曲总长时才能确定结束时间
        var songDuration = (int)Math.Round(songDurationMs);
        if (songDuration > cursor) AddGap(gaps, lines, cursor, songDuration, minimumDurationMs);

        return gaps;
    }

    /// <summary>
    /// 找到播放位置所在的间隙
    /// </summary>
    public static LyricsGapSegment? FindGapAt(IReadOnlyList<LyricsGapSegment>? gaps, double timeMs)
    {
        if (gaps == null) return null;

        foreach (var gap in gaps)
            if (timeMs >= gap.StartMs && timeMs < gap.EndMs)
                return gap;

        return null;
    }

    private static void AddGap(
        List<LyricsGapSegment> gaps,
        IReadOnlyList<LyricsGapLine> lines,
        int startMs,
        int endMs,
        int minimumDurationMs)
    {
        if (endMs <= startMs) return;
        if (endMs - startMs < minimumDurationMs) return;

        var previousLineIndex = -1;
        var nextLineIndex = -1;
        var markerLineIndex = -1;
        var previousLineStart = int.MinValue;
        var nextLineStart = int.MaxValue;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];

            if (line.IsEmpty)
            {
                // 空行必须落在间隙内，且覆盖到间隙结束，才算这段间隙的标记行
                if (markerLineIndex < 0 &&
                    line.StartMs >= startMs && line.StartMs < endMs &&
                    (line.EndMs ?? int.MaxValue) >= endMs)
                    markerLineIndex = i;
                continue;
            }

            if (line.StartMs <= startMs)
            {
                if (line.StartMs >= previousLineStart)
                {
                    previousLineStart = line.StartMs;
                    previousLineIndex = i;
                }
            }
            else if (line.StartMs >= endMs)
            {
                if (line.StartMs < nextLineStart)
                {
                    nextLineStart = line.StartMs;
                    nextLineIndex = i;
                }
            }
        }

        gaps.Add(new LyricsGapSegment(startMs, endMs, markerLineIndex, previousLineIndex, nextLineIndex));
    }
}
