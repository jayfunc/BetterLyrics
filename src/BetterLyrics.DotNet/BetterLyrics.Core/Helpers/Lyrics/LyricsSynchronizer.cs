using BetterLyrics.Core.Models.Lyrics;

namespace BetterLyrics.Core.Helpers.Lyrics;

public class LyricsSynchronizer
{
    private int _lastFoundIndex;

    public void Reset()
    {
        _lastFoundIndex = 0;
    }

    public int GetCurrentLineIndex(double currentTimeMs, IList<BaseRenderLyricsLine>? lines)
    {
        if (lines == null || lines.Count == 0) return 0;

        if (_lastFoundIndex >= 0 && _lastFoundIndex < lines.Count)
        {
            var lastLine = lines[_lastFoundIndex];
            if (lastLine.LaneIndex == 0 && IsTimeInLine(currentTimeMs, lines, _lastFoundIndex)) return _lastFoundIndex;
        }

        var bestCandidateIndex = -1;
        var bestCandidateLane = int.MaxValue;

        for (var i = 0; i < lines.Count; i++)
            if (IsTimeInLine(currentTimeMs, lines, i))
            {
                var currentLine = lines[i];
                var currentLane = currentLine.LaneIndex;

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

        if (bestCandidateIndex != -1)
        {
            _lastFoundIndex = bestCandidateIndex;
            return bestCandidateIndex;
        }

        return Math.Min(_lastFoundIndex, lines.Count - 1);
    }

    private static bool IsTimeInLine(double time, IList<BaseRenderLyricsLine> lines, int index)
    {
        if (index < 0 || index >= lines.Count) return false;
        var line = lines[index];
        var nextLine = index + 1 < lines.Count ? lines[index + 1] : null;
        if (time < line.StartMs) return false;
        if (nextLine != null && time >= nextLine.StartMs) return false;
        return true;
    }
}