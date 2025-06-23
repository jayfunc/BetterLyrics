using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ATL;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;

namespace BetterLyrics.WinUI3.Helper
{
    public class LyricsParser
    {
        private List<LyricsLine> _lyricsLines = [];

        public List<LyricsLine> Parse(
            string raw,
            LyricsFormat? lyricsFormat = null,
            string? title = null,
            string? artist = null,
            int durationMs = 0
        )
        {
            _lyricsLines = [];
            switch (lyricsFormat)
            {
                case LyricsFormat.Lrc:
                case LyricsFormat.Eslrc:
                    ParseLrc(raw, durationMs);
                    break;
                case LyricsFormat.Ttml:
                    ParseTtml(raw, durationMs);
                    break;
                default:
                    break;
            }

            if (_lyricsLines.Count > 0 && _lyricsLines[0].StartMs > 0)
            {
                _lyricsLines.Insert(
                    0,
                    new LyricsLine
                    {
                        StartMs = 0,
                        EndMs = _lyricsLines[0].StartMs,
                        Texts = [""],
                        CharTimings = [],
                    }
                );
            }
            return _lyricsLines;
        }

        private void ParseLrc(string raw, int durationMs)
        {
            var lines = raw.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            var lrcLines =
                new List<(int time, string text, List<(int time, string text)> syllables)>();

            // 支持 [mm:ss.xx]字、<mm:ss.xx>字，毫秒两位或三位
            var syllableRegex = new Regex(
                @"(\[|\<)(\d{2}):(\d{2})\.(\d{2,3})(\]|\>)([^\[\]\<\>]*)"
            );

            foreach (var line in lines)
            {
                var matches = syllableRegex.Matches(line);
                var syllables = new List<(int, string)>();
                foreach (Match m in matches)
                {
                    int min = int.Parse(m.Groups[2].Value);
                    int sec = int.Parse(m.Groups[3].Value);
                    int ms = int.Parse(m.Groups[4].Value.PadRight(3, '0'));
                    int totalMs = min * 60_000 + sec * 1000 + ms;
                    string text = m.Groups[6].Value;
                    syllables.Add((totalMs, text));
                }
                if (syllables.Count > 0)
                {
                    lrcLines.Add(
                        (
                            syllables[0].Item1,
                            string.Concat(syllables.Select(s => s.Item2)),
                            syllables
                        )
                    );
                }
                else
                {
                    // 普通LRC行
                    var bracketRegex = new Regex(@"\[(\d{2}):(\d{2})\.(\d{2,3})\]");
                    var bracketMatches = bracketRegex.Matches(line);
                    string content = line;
                    int? lineStartTime = null;
                    if (bracketMatches.Count > 0)
                    {
                        var m = bracketMatches[0];
                        int min = int.Parse(m.Groups[1].Value);
                        int sec = int.Parse(m.Groups[2].Value);
                        int ms = int.Parse(m.Groups[3].Value.PadRight(3, '0'));
                        lineStartTime = min * 60_000 + sec * 1000 + ms;
                        content = bracketRegex.Replace(line, "").Trim();
                        lrcLines.Add((lineStartTime.Value, content, new List<(int, string)>()));
                    }
                }
            }

            // 按时间排序
            lrcLines = lrcLines.OrderBy(l => l.time).ToList();

            // 构建 LyricsLine
            for (int i = 0; i < lrcLines.Count; i++)
            {
                var (start, text, syllables) = lrcLines[i];
                var line = new LyricsLine
                {
                    StartMs = start,
                    EndMs = (i + 1 < lrcLines.Count) ? lrcLines[i + 1].time : durationMs,
                    Texts = [text],
                    CharTimings = [],
                };

                if (syllables != null && syllables.Count > 0)
                {
                    for (int j = 0; j < syllables.Count; j++)
                    {
                        var (charStart, charText) = syllables[j];
                        int charEnd =
                            (j + 1 < syllables.Count) ? syllables[j + 1].Item1 : line.EndMs;
                        if (!string.IsNullOrEmpty(charText))
                        {
                            line.CharTimings.Add(
                                new CharTiming { StartMs = charStart, EndMs = charEnd }
                            );
                        }
                    }
                }
                _lyricsLines.Add(line);
            }
        }

        private void ParseTtml(string raw, int durationMs)
        {
            // 简单 TTML 解析
            try
            {
                var xdoc = XDocument.Parse(raw);
                XNamespace ns = xdoc.Root?.Name.Namespace ?? "";
                var body = xdoc.Descendants(ns + "body").FirstOrDefault();
                if (body == null)
                    return;
                var ps = body.Descendants(ns + "p");
                foreach (var p in ps)
                {
                    string text = p.Value.Trim();
                    string? begin = p.Attribute("begin")?.Value;
                    string? end = p.Attribute("end")?.Value;
                    int startMs = ParseTtmlTime(begin);
                    int endMs = ParseTtmlTime(end);
                    _lyricsLines.Add(
                        new LyricsLine
                        {
                            StartMs = startMs,
                            EndMs = endMs,
                            Texts = [text],
                            CharTimings = [],
                        }
                    );
                }
            }
            catch
            {
                // 解析失败，忽略
            }
        }

        private int ParseTtmlTime(string? t)
        {
            if (string.IsNullOrEmpty(t))
                return 0;
            // 支持 "00:00:01.000" 或 "1.000s"
            if (t.EndsWith("s"))
            {
                if (double.TryParse(t.TrimEnd('s'), out double seconds))
                    return (int)(seconds * 1000);
            }
            else
            {
                var parts = t.Split(':');
                if (parts.Length == 3)
                {
                    int h = int.Parse(parts[0]);
                    int m = int.Parse(parts[1]);
                    double s = double.Parse(parts[2]);
                    return (int)((h * 3600 + m * 60 + s) * 1000);
                }
            }
            return 0;
        }
    }
}
