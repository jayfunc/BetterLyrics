using BetterLyrics.WinUI3.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Logic
{
    public class LyricsSynchronizer
    {
        private int _lastFoundIndex = 0;

        public void Reset()
        {
            _lastFoundIndex = 0;
        }

        public int GetCurrentLineIndex(double currentTimeMs, LyricsData? lyricsData)
        {
            if (lyricsData == null || lyricsData.LyricsLines.Count == 0) return 0;
            var lines = lyricsData.LyricsLines;

            // Cache hit
            if (IsTimeInLine(currentTimeMs, lines, _lastFoundIndex)) return _lastFoundIndex;
            if (_lastFoundIndex + 1 < lines.Count && IsTimeInLine(currentTimeMs, lines, _lastFoundIndex + 1))
            {
                _lastFoundIndex++;
                return _lastFoundIndex;
            }

            // Cache miss
            for (int i = 0; i < lines.Count; i++)
            {
                if (IsTimeInLine(currentTimeMs, lines, i))
                {
                    _lastFoundIndex = i;
                    return i;
                }
            }

            // Default
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

        private bool IsTimeInLine(double time, IList<LyricsLine> lines, int index)
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
