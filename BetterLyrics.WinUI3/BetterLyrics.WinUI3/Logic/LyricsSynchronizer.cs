using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;

namespace BetterLyrics.WinUI3.Logic
{
    public class LyricsSynchronizer
    {
        private int _lastFoundIndex = 0;

        public void Reset()
        {
            _lastFoundIndex = 0;
        }

        public int GetCurrentLineIndex(double currentTimeMs, IList<RenderLyricsLine>? lines)
        {
            if (lines == null || lines.Count == 0) return 0;

            if (_lastFoundIndex >= 0 && _lastFoundIndex < lines.Count)
            {
                var lastLine = lines[_lastFoundIndex];
                if (lastLine.LaneIndex == 0 && IsTimeInLine(currentTimeMs, lines, _lastFoundIndex))
                {
                    return _lastFoundIndex;
                }
            }

            int bestCandidateIndex = -1;
            int bestCandidateLane = int.MaxValue;

            for (int i = 0; i < lines.Count; i++)
            {
                if (IsTimeInLine(currentTimeMs, lines, i))
                {
                    var currentLine = lines[i];
                    int currentLane = currentLine.LaneIndex;

                    if (currentLane == 0)
                    {
                        _lastFoundIndex = i;
                        return i;
                    }

                    if (currentLane < bestCandidateLane)
                    {
                        bestCandidateIndex = i;
                        bestCandidateLane = currentLane;
                    }
                }
                else if (lines[i].StartMs > currentTimeMs + 1000)
                {
                    break;
                }
            }

            if (bestCandidateIndex != -1)
            {
                _lastFoundIndex = bestCandidateIndex;
                return bestCandidateIndex;
            }

            return Math.Min(_lastFoundIndex, lines.Count - 1);
        }

        public LinePlaybackState GetLinePlayingProgress(
            double currentTimeMs,
            LyricsLine line,
            LyricsLine? nextLine,
            double songDurationMs,
            bool isForceWordByWord)
        {
            var state = new LinePlaybackState { SyllableStartIndex = 0, SyllableLength = 0, SyllableProgress = 0 };

            if (line == null) return state;

            double lineEndMs;
            if (line.EndMs != null) lineEndMs = line.EndMs.Value;
            else if (nextLine != null) lineEndMs = nextLine.StartMs;
            else lineEndMs = songDurationMs;

            // 还没到
            if (currentTimeMs < line.StartMs) return state;

            // 过了
            if (currentTimeMs > lineEndMs)
            {
                state.SyllableProgress = 1f;
                state.SyllableStartIndex = Math.Max(0, line.OriginalText.Length - 1);
                state.SyllableLength = 1;
                return state;
            }

            // 逐字
            if (line.LyricsSyllables != null && line.LyricsSyllables.Count > 1)
            {
                return CalculateSyllableProgress(currentTimeMs, line, lineEndMs);
            }

            // 强制逐字
            if (isForceWordByWord && line.OriginalText.Length > 0)
            {
                return CalculateSimulatedProgress(currentTimeMs, line, lineEndMs);
            }
            else
            {
                // 普通行
                state.SyllableStartIndex = line.OriginalText.Length;
                state.SyllableProgress = 1f;
                return state;
            }
        }

        private LinePlaybackState CalculateSyllableProgress(double time, LyricsLine line, double lineEndMs)
        {
            var state = new LinePlaybackState();
            int count = line.LyricsSyllables.Count;

            for (int i = 0; i < count; i++)
            {
                var timing = line.LyricsSyllables[i];
                var nextTiming = (i + 1 < count) ? line.LyricsSyllables[i + 1] : null;

                double timingEndMs = timing.EndMs ?? nextTiming?.StartMs ?? lineEndMs;

                // 在当前字范围内
                if (time >= timing.StartMs && time <= timingEndMs)
                {
                    state.SyllableStartIndex = timing.StartIndex;
                    state.SyllableLength = timing.Text.Length;
                    state.SyllableProgress = (timingEndMs > timing.StartMs)
                        ? (time - timing.StartMs) / (timingEndMs - timing.StartMs)
                        : 0;
                    return state;
                }
                // 在空隙中 (已过当前字，未到下个字)
                else if (time > timingEndMs && (nextTiming == null || time < nextTiming.StartMs))
                {
                    state.SyllableProgress = 1f; // 保持上个字满进度
                    state.SyllableStartIndex = timing.StartIndex;
                    state.SyllableLength = timing.Text.Length;
                    return state;
                }
            }
            return state;
        }

        private LinePlaybackState CalculateSimulatedProgress(double time, LyricsLine line, double lineEndMs)
        {
            var state = new LinePlaybackState();
            int textLength = line.OriginalText.Length;

            double progress = (time - line.StartMs) / (lineEndMs - line.StartMs);
            progress = Math.Clamp(progress, 0, 1);

            double charFloatIndex = progress * textLength;
            int charIndex = (int)charFloatIndex;

            state.SyllableStartIndex = Math.Clamp(charIndex, 0, textLength - 1);
            state.SyllableLength = 1;
            state.SyllableProgress = charFloatIndex - charIndex;

            return state;
        }

        private bool IsTimeInLine(double time, IList<RenderLyricsLine> lines, int index)
        {
            if (index < 0 || index >= lines.Count) return false;
            var line = lines[index];
            var nextLine = (index + 1 < lines.Count) ? lines[index + 1] : null;
            if (time < line.StartMs) return false;
            if (nextLine != null && time >= nextLine.StartMs) return false;
            return true;
        }
    }
}
