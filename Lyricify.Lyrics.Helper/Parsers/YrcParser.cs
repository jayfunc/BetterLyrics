using Lyricify.Lyrics.Models;
using Lyricify.Lyrics.Parsers.Models.Yrc;
using Newtonsoft.Json;
using System.Text;

namespace Lyricify.Lyrics.Parsers
{
    public static class YrcParser
    {
        public static LyricsData Parse(string input)
        {
            var lyricsData = new LyricsData()
            {
                File = new()
                {
                    Type = LyricsTypes.Yrc,
                    SyncTypes = SyncTypes.SyllableSynced,
                },
            };
            var lines = new List<ILineInfo>();

            // 处理信息行
            var i = 0;
            for (; i < input.Length; i++)
            {
                if (input[i] == '{')
                {
                    var endIndex = input.IndexOf('\n', i);
                    var jsonLine = input[i..endIndex];
                    var credits = JsonConvert.DeserializeObject<CreditsInfo>(jsonLine);
                    if (credits != null)
                    {
                        lines.Add(new LineInfo(string.Concat(credits.Credits.Select(c => c.Text)), credits.Timestamp));
                        lyricsData.File.SyncTypes = SyncTypes.MixedSynced; // 有信息行，说明是混合同步

                        if (credits.Credits is { Count: > 0 } && credits.Credits[0].Text.StartsWith("作词"))
                        {
                            lyricsData.Writers = credits.Credits
                                .GetRange(1, credits.Credits.Count - 1)
                                .Select(c => c.Text)
                                .Where(t => t != "/")
                                .ToList();
                        }
                    }
                    i = endIndex;
                }
                else if (input[i] == '\n' || input[i] == '\r')
                {
                    continue;
                }
                else
                {
                    break;
                }
            }

            int j = input.Length - 1;
            while (j >= 0 && (input[j] == '\n' || input[j] == '\r')) j--;
            var endCredits = new List<ILineInfo>();
            for (; j >= 0; j--)
            {
                if (input[j] == '}')
                {
                    var startIndex = input.LastIndexOf('\n', j);
                    if (startIndex == -1) startIndex = 0; else startIndex++;
                    var jsonLine = input[startIndex..(j + 1)];
                    var credits = JsonConvert.DeserializeObject<CreditsInfo>(jsonLine);
                    if (credits != null)
                    {
                        endCredits.Add(new LineInfo(string.Concat(credits.Credits.Select(c => c.Text)), credits.Timestamp));
                        lyricsData.File.SyncTypes = SyncTypes.MixedSynced; // 有信息行，说明是混合同步

                        if (credits.Credits is { Count: > 0 } && credits.Credits[0].Text.StartsWith("作词"))
                        {
                            lyricsData.Writers = credits.Credits
                                .GetRange(1, credits.Credits.Count - 1)
                                .Select(c => c.Text)
                                .Where(t => t != "/")
                                .ToList();
                        }
                    }
                    j = startIndex - 1;
                }
                else if (input[j] == '\n' || input[j] == '\r')
                {
                    continue;
                }
                else
                {
                    break;
                }
            }

            // 处理歌词行
            var lyricsSpan = input.AsSpan(i, j + 1 - i);
            var lyricsList = ParseOnlyLyrics(lyricsSpan);

            lines.AddRange(lyricsList);
            endCredits.Reverse();
            lines.AddRange(endCredits);

            lyricsData.Lines = lines;
            return lyricsData;
        }

        /// <summary>
        /// 解析 YRC 歌词 (可包含信息行)
        /// </summary>
        public static List<ILineInfo> ParseLyrics(string input)
        {
            var lines = new List<ILineInfo>();

            // 处理信息行
            var i = 0;
            for (; i < input.Length; i++)
            {
                if (input[i] == '{')
                {
                    var endIndex = input.IndexOf('\n', i);
                    if (endIndex == -1) endIndex = input.Length;
                    var jsonLine = input[i..endIndex];
                    var credits = JsonConvert.DeserializeObject<CreditsInfo>(jsonLine);
                    if (credits != null)
                    {
                        lines.Add(new LineInfo(string.Concat(credits.Credits.Select(c => c.Text)), credits.Timestamp));
                    }
                    i = endIndex;
                }
                else if (input[i] == '\n' || input[i] == '\r')
                {
                    continue;
                }
                else
                {
                    break;
                }
            }

            int j = input.Length - 1;
            while (j >= 0 && (input[j] == '\n' || input[j] == '\r')) j--;
            for (; j >= 0; j--)
            {
                if (input[j] == '}')
                {
                    // 按照原始逻辑，不解析末尾的信息行，只用于定位
                    var startIndex = input.LastIndexOf('\n', j);
                    if (startIndex == -1) startIndex = 0; else startIndex++;

                    j = startIndex - 1;
                }
                else if (input[j] == '\n' || input[j] == '\r')
                {
                    continue;
                }
                else
                {
                    break;
                }
            }
            if (j < i) j = i - 1;

            // 处理歌词行
            var lyricsSpan = input.AsSpan(i, j + 1 - i);
            var lyricsList = ParseOnlyLyrics(lyricsSpan);

            lines.AddRange(lyricsList);
            return lines;
        }

        /// <summary>
        /// 解析 YRC 歌词 (不含信息行)
        /// </summary>
        public static List<ILineInfo> ParseOnlyLyrics(ReadOnlySpan<char> input)
        {
            var lines = new List<SyllableLineInfo>();
            var karaokeWordInfos = new List<ISyllableInfo>();
            var timeSpanBuilder = 0;
            var lyricStringBuilder = new StringBuilder();
            var wordTimespan = 0;
            var wordDuration = 0;
            var state = CurrentState.None;
            var reachesEnd = false;
            for (var i = 0; i < input.Length; i++)
            {
                if (i != input.Length)
                {
                    ref readonly var curChar = ref input[i];

                    if (curChar == '\n' || curChar == '\r' || i + 1 == input.Length)
                    {
                        if (i + 1 < input.Length)
                        {
                            if (input[i + 1] == '\n' || input[i + 1] == '\r') i++;
                            karaokeWordInfos.Add(new SyllableInfo(lyricStringBuilder.ToString(), wordTimespan, wordTimespan + wordDuration));
                            lines.Add(new SyllableLineInfo(karaokeWordInfos));
                            karaokeWordInfos.Clear();
                            lyricStringBuilder.Clear();
                            state = CurrentState.None;
                            continue;
                        }
                        if (i + 1 == input.Length)
                        {
                            reachesEnd = true;
                        }
                    }
                    switch (curChar)
                    {
                        case '[':
                            if (state == CurrentState.Lyric)
                            {
                                if (i + 1 < input.Length)
                                {
                                    if (!char.IsNumber(input[i + 1])) break;
                                }
                            }
                            state = CurrentState.PossiblyLyricTimestamp;
                            continue;
                        case ',':
                            if (state == CurrentState.Lyric)
                            {
                                if (i + 1 < input.Length)
                                {
                                    if (!char.IsNumber(input[i + 1])) break;
                                }
                            }
                            if (state == CurrentState.LyricTimestamp)
                            {
                                state = CurrentState.PossiblyLyricDuration;
                                timeSpanBuilder = 0;
                            }
                            else if (state == CurrentState.WordTimestamp)
                            {
                                state = CurrentState.PossiblyWordDuration;
                                wordTimespan = timeSpanBuilder;
                                timeSpanBuilder = 0;
                            }
                            else
                            {
                                state = CurrentState.WordUnknownItem;
                                wordDuration = timeSpanBuilder;
                                timeSpanBuilder = 0;
                            }
                            continue;
                        case ']':
                            if (state == CurrentState.Lyric)
                            {
                                if (i + 1 < input.Length)
                                {
                                    if (!char.IsNumber(input[i + 1])) break;
                                }
                            }
                            state = CurrentState.None;
                            timeSpanBuilder = 0;
                            continue;
                        case '(':
                            if (state == CurrentState.Lyric)
                            {
                                if (i + 1 < input.Length)
                                {
                                    if (!char.IsNumber(input[i + 1])) break;
                                }
                                karaokeWordInfos.Add(new SyllableInfo(lyricStringBuilder.ToString(), wordTimespan, wordTimespan + wordDuration));
                                lyricStringBuilder.Clear();
                            }
                            state = CurrentState.PossiblyWordTimestamp;
                            continue;
                        case ')':
                            if (state == CurrentState.Lyric)
                            {
                                if (i + 1 < input.Length)
                                {
                                    if (!char.IsNumber(input[i + 1])) break;
                                }
                            }
                            state = CurrentState.Lyric;
                            continue;
                    }
                    switch (state)
                    {
                        case CurrentState.PossiblyLyricTimestamp:
                            if (char.IsNumber(curChar)) state = CurrentState.LyricTimestamp;
                            timeSpanBuilder *= 10;
                            timeSpanBuilder += curChar - '0';
                            break;
                        case CurrentState.LyricTimestamp:
                            timeSpanBuilder *= 10;
                            timeSpanBuilder += curChar - '0';
                            break;
                        case CurrentState.PossiblyWordTimestamp:
                            if (char.IsNumber(curChar)) state = CurrentState.WordTimestamp;
                            timeSpanBuilder *= 10;
                            timeSpanBuilder += curChar - '0';
                            break;
                        case CurrentState.WordTimestamp:
                            timeSpanBuilder *= 10;
                            timeSpanBuilder += curChar - '0';
                            break;
                        case CurrentState.PossiblyLyricDuration:
                            if (char.IsNumber(curChar)) state = CurrentState.LyricDuration;
                            timeSpanBuilder *= 10;
                            timeSpanBuilder += curChar - '0';
                            break;
                        case CurrentState.LyricDuration:
                            timeSpanBuilder *= 10;
                            timeSpanBuilder += curChar - '0';
                            break;
                        case CurrentState.PossiblyWordDuration:
                            if (char.IsNumber(curChar)) state = CurrentState.WordDuration;
                            timeSpanBuilder *= 10;
                            timeSpanBuilder += curChar - '0';
                            break;
                        case CurrentState.WordDuration:
                            timeSpanBuilder *= 10;
                            timeSpanBuilder += curChar - '0';
                            break;
                        case CurrentState.Lyric:
                            if (reachesEnd && (input[i] == '\n' || input[i] == '\r')) break;
                            lyricStringBuilder.Append(curChar);
                            break;
                    }
                    if (reachesEnd)
                    {
                        karaokeWordInfos.Add(new SyllableInfo(lyricStringBuilder.ToString(), wordTimespan, wordTimespan + wordDuration));
                        lines.Add(new SyllableLineInfo(karaokeWordInfos));
                        karaokeWordInfos.Clear();
                        lyricStringBuilder.Clear();
                    }
                }
            }
            return lines.Cast<ILineInfo>().ToList();
        }

        private enum CurrentState
        {
            None,
            LyricTimestamp,
            WordTimestamp,
            LyricDuration,
            WordDuration,
            WordUnknownItem,
            PossiblyLyricDuration,
            PossiblyWordDuration,
            PossiblyLyricTimestamp,
            PossiblyWordTimestamp,
            Lyric
        }
    }
}
