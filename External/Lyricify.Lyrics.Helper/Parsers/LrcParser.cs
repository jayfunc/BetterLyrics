using Lyricify.Lyrics.Models;
using System.Buffers;

namespace Lyricify.Lyrics.Parsers
{
    public static class LrcParser
    {
        public static LyricsData Parse(ReadOnlySpan<char> input)
        {
            var lines = new List<LineInfo>();
            var attributeName = string.Empty;
            var attributes = new List<KeyValuePair<string, string>>();
            var trackMetadata = new TrackMetadata();
            var curStateStartPosition = 0;
            var timeCalculationCache = 0;
            var curTimestamps = ArrayPool<int>.Shared.Rent(64); // Max Count
            int curTimestamp = 0;
            int currentTimestampPosition = 0;
            var offset = 0;
            var reachesEnd = false;
            var lastCharacterIsLineBreak = false;
            var state = CurrentState.None;
            var timeStampType = TimeStampType.None;
            for (var i = 0; i < input.Length; i++)
            {
                ref readonly var curChar = ref input[i];
                // 剥离开, 方便 分支预测
                if (state == CurrentState.Lyric)
                {
                    if (curChar != '\n' && curChar != '\r' && i + 1 < input.Length)
                    {
                        continue;
                    }
                    else
                    {
                        if (i + 1 < input.Length)
                        {
                            for (int j = 0; j < curTimestamps.Length; j++)
                            {
                                if (curTimestamps[j] == -1) break;
                                lines.Add(new LineInfo(
                                    input.Slice(curStateStartPosition + 1, i - curStateStartPosition - 1).ToString().Trim(),
                                    curTimestamps[j] - offset));
                            }
                            if (input[i + 1] == '\n' || input[i + 1] == '\r') i++;
                            currentTimestampPosition = 0;
                            // Change State
                            state = CurrentState.None;
                            continue;
                        }
                        if (i + 1 == input.Length)
                        {
                            reachesEnd = true;
                            if (curChar == '\r' || curChar == '\n')
                            {
                                lastCharacterIsLineBreak = true;
                            }
                        }
                    }
                }
                if (reachesEnd && state == CurrentState.Lyric)
                {
                    for (int j = 0; j < curTimestamps.Length; j++)
                    {
                        if (curTimestamps[j] == -1) break;
                        lines.Add(new LineInfo(
                            input.Slice(curStateStartPosition + 1, i - curStateStartPosition - (lastCharacterIsLineBreak ? 1 : 0)).ToString().Trim(),
                            curTimestamps[j] - offset));
                    }
                    continue;
                }
                switch (state)
                {
                    case CurrentState.PossiblyLyric:
                        if (curChar == '[')
                        {
                            state = CurrentState.AwaitingStateLyric;
                        }
                        else
                        {
                            i -= 1;
                            state = CurrentState.Lyric;
                        }

                        break;
                    case CurrentState.None:
                        Array.Fill(curTimestamps, -1);
                        if (curChar == '[')
                        {
                            state = CurrentState.AwaitingState;
                        }

                        break;
                    case CurrentState.AwaitingState:
                        if ('0' <= curChar && curChar <= '9')
                        {
                            // Time
                            state = CurrentState.Timestamp;
                            timeStampType = TimeStampType.Minutes;
                            curStateStartPosition = i;
                            curTimestamp = 0;
                            i--;
                        }
                        else
                        {
                            state = CurrentState.Attribute;
                            curStateStartPosition = i;
                        }

                        break;
                    case CurrentState.AwaitingStateLyric:
                        if ('0' <= curChar && curChar <= '9')
                        {
                            // Time
                            state = CurrentState.Timestamp;
                            timeStampType = TimeStampType.Minutes;
                            curStateStartPosition = i;
                            curTimestamp = 0;
                            i--;
                        }
                        else
                        {
                            state = CurrentState.Lyric;
                            curStateStartPosition = i - 2;
                        }

                        break;
                    case CurrentState.Attribute:
                        if (curChar == ':')
                        {
                            attributeName = input[curStateStartPosition..i].ToString();
                            curStateStartPosition = i + 1;
                            state = CurrentState.AttributeContent;
                        }

                        break;
                    case CurrentState.AttributeContent:
                        if (curChar == ']')
                        {
                            string attributeValue;
                            if (attributeName == "offset")
                            {
                                offset = timeCalculationCache;
                                timeCalculationCache = 0;
                                attributeValue = offset.ToString();
                            }
                            else
                            {
                                attributeValue = input[curStateStartPosition..i].ToString();
                            }
                            var attribute = new KeyValuePair<string, string>(attributeName, attributeValue);
                            // 将 Attribute 扔到 TrackMetadata 中
                            switch (attribute.Key)
                            {
                                case "ar":
                                    trackMetadata.Artist = attribute.Value;
                                    break;
                                case "al":
                                    trackMetadata.Album = attribute.Value;
                                    break;
                                case "ti":
                                    trackMetadata.Title = attribute.Value;
                                    break;
                                case "length":
                                    if (int.TryParse(attribute.Value, out int result))
                                        trackMetadata.DurationMs = result;
                                    break;
                            }
                            attributes.Add(attribute);
                            attributeName = string.Empty;
                            state = CurrentState.None;
                            break;
                        }
                        if (attributeName == "offset")
                        {
                            timeCalculationCache = timeCalculationCache * 10 + curChar - '0';
                            continue;
                        }
                        break;
                    case CurrentState.Timestamp:
                        if (timeStampType == TimeStampType.Milliseconds)
                        {
                            if (curChar != ']')
                            {
                                timeCalculationCache = timeCalculationCache * 10 + curChar - '0';
                                continue;
                            }
                            else
                            {
                                var pow = i - curStateStartPosition - 1; // 几位小数
                                curTimestamp += timeCalculationCache * (int)Math.Pow(10, 3 - pow);
                                if (currentTimestampPosition + 1 >= curTimestamps.Length)
                                    curTimestamps = ExtendIntArrayPool(curTimestamps);
                                curTimestamps[currentTimestampPosition++] = curTimestamp;
                                timeStampType = TimeStampType.None;
                                timeCalculationCache = 0;
                                curStateStartPosition = i;
                                curTimestamp = 0;
                                state = CurrentState.PossiblyLyric;
                                continue;
                            }
                        }
                        switch (curChar)
                        {
                            case ':':
                            case '.':
                                if (timeStampType == TimeStampType.Minutes)
                                {
                                    curTimestamp = (curTimestamp + timeCalculationCache) * 60;
                                    timeCalculationCache = 0;
                                    timeStampType = TimeStampType.Seconds;
                                    continue;
                                }
                                if (timeStampType == TimeStampType.Seconds)
                                {
                                    curTimestamp = (curTimestamp + timeCalculationCache) * 1000;
                                    curStateStartPosition = i;
                                    timeCalculationCache = 0;
                                    timeStampType = TimeStampType.Milliseconds;
                                    continue;
                                }
                                throw new ArgumentOutOfRangeException();
                            case ']':
                                // 无毫秒数需要从此跳出
                                if (currentTimestampPosition + 1 >= curTimestamps.Length)
                                    curTimestamps = ExtendIntArrayPool(curTimestamps);
                                curTimestamps[currentTimestampPosition++] = (curTimestamp + timeCalculationCache) * 1000;
                                timeCalculationCache = 0;
                                curStateStartPosition = i;
                                curTimestamp = 0;
                                state = CurrentState.PossiblyLyric;
                                timeStampType = TimeStampType.None;
                                continue;
                            default:
                                timeCalculationCache = timeCalculationCache * 10 + curChar - '0';
                                break;
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            ArrayPool<int>.Shared.Return(curTimestamps);
            lines.Sort();

            var lyricsData = new LyricsData
            {
                TrackMetadata = trackMetadata,
                Lines = lines.Cast<ILineInfo>().ToList(),
                File = new()
                {
                    SyncTypes = SyncTypes.LineSynced,
                    Type = LyricsTypes.Lrc,
                }
            };
            return lyricsData;
        }

        public static List<ILineInfo> ParseLyrics(ReadOnlySpan<char> input)
        {
            var lines = new List<LineInfo>();
            var curStateStartPosition = 0;
            var timeCalculationCache = 0;
            var curTimestamps = ArrayPool<int>.Shared.Rent(64); // Max Count 
            int curTimestamp = 0;
            int currentTimestampPosition = 0;
            var reachesEnd = false;
            var lastCharacterIsLineBreak = false;
            var state = CurrentState.None;
            var timeSpanType = TimeStampType.None;
            for (var i = 0; i < input.Length; i++)
            {
                ref readonly var curChar = ref input[i];
                // 剥离开, 方便 分支预测
                if (state == CurrentState.Lyric)
                {
                    if (curChar != '\n' && curChar != '\r' && i + 1 < input.Length)
                    {
                        continue;
                    }
                    else
                    {
                        if (i + 1 < input.Length)
                        {
                            for (int j = 0; j < curTimestamps.Length; j++)
                            {
                                if (curTimestamps[j] == -1) break;
                                lines.Add(new LineInfo(
                                    input.Slice(curStateStartPosition + 1, i - curStateStartPosition - 1).ToString().Trim(),
                                    curTimestamps[j]));
                            }
                            if (input[i + 1] == '\n' || input[i + 1] == '\r') i++;
                            currentTimestampPosition = 0;
                            // Change State
                            state = CurrentState.None;
                            continue;
                        }
                        if (i + 1 == input.Length)
                        {
                            reachesEnd = true;
                            if (curChar == '\r' || curChar == '\n')
                            {
                                lastCharacterIsLineBreak = true;
                            }
                        }
                    }
                }
                if (reachesEnd)
                {
                    for (int j = 0; j < curTimestamps.Length; j++)
                    {
                        if (curTimestamps[j] == -1) break;
                        lines.Add(new LineInfo(
                            input.Slice(curStateStartPosition + 1, i - curStateStartPosition - (lastCharacterIsLineBreak ? 1 : 0)).ToString().Trim(),
                            curTimestamps[j]));
                    }
                    continue;
                }
                switch (state)
                {
                    case CurrentState.PossiblyLyric:
                        if (curChar == '[')
                        {
                            state = CurrentState.AwaitingStateLyric;
                        }
                        else
                        {
                            i -= 1;
                            state = CurrentState.Lyric;
                        }

                        break;
                    case CurrentState.None:
                        Array.Fill(curTimestamps, -1);
                        if (curChar == '[')
                        {
                            state = CurrentState.AwaitingState;
                        }

                        break;
                    case CurrentState.AwaitingState:
                        if ('0' <= curChar && curChar <= '9')
                        {
                            // Time
                            state = CurrentState.Timestamp;
                            timeSpanType = TimeStampType.Minutes;
                            curStateStartPosition = i;
                            curTimestamp = 0;
                            i--;
                        }
                        else
                        {
                            state = CurrentState.Attribute;
                            curStateStartPosition = i;
                        }

                        break;
                    case CurrentState.AwaitingStateLyric:
                        if ('0' <= curChar && curChar <= '9')
                        {
                            // Time
                            state = CurrentState.Timestamp;
                            timeSpanType = TimeStampType.Minutes;
                            curStateStartPosition = i;
                            curTimestamp = 0;
                            i--;
                        }
                        else
                        {
                            state = CurrentState.Lyric;
                            curStateStartPosition = i - 2;
                        }

                        break;
                    case CurrentState.Attribute:
                        if (curChar == ':')
                        {
                            //var attr = input.Slice(curStateStartPosition, i - curStateStartPosition);
                            state = CurrentState.AttributeContent;
                        }

                        break;
                    case CurrentState.AttributeContent:
                        if (curChar == ']') state = CurrentState.None;
                        break;
                    case CurrentState.Timestamp:
                        if (timeSpanType == TimeStampType.Milliseconds)
                        {
                            if (curChar != ']')
                            {
                                timeCalculationCache = timeCalculationCache * 10 + curChar - '0';
                                continue;
                            }
                            else
                            {
                                var pow = i - curStateStartPosition - 1; // 几位小数
                                curTimestamp += timeCalculationCache * (int)Math.Pow(10, 3 - pow);
                                if (currentTimestampPosition + 1 >= curTimestamps.Length)
                                    curTimestamps = ExtendIntArrayPool(curTimestamps);
                                curTimestamps[currentTimestampPosition++] = curTimestamp;
                                timeSpanType = TimeStampType.None;
                                timeCalculationCache = 0;
                                curStateStartPosition = i;
                                curTimestamp = 0;
                                state = CurrentState.PossiblyLyric;
                                continue;
                            }
                        }
                        switch (curChar)
                        {
                            case ':':
                            case '.':
                                if (timeSpanType == TimeStampType.Minutes)
                                {
                                    curTimestamp = (curTimestamp + timeCalculationCache) * 60;
                                    timeCalculationCache = 0;
                                    timeSpanType = TimeStampType.Seconds;
                                    continue;
                                }
                                if (timeSpanType == TimeStampType.Seconds)
                                {
                                    curTimestamp = (curTimestamp + timeCalculationCache) * 1000;
                                    curStateStartPosition = i;
                                    timeCalculationCache = 0;
                                    timeSpanType = TimeStampType.Milliseconds;
                                    continue;
                                }
                                throw new ArgumentOutOfRangeException();
                            case ']':
                                // 无毫秒数需要从此跳出
                                if (currentTimestampPosition + 1 >= curTimestamps.Length)
                                    curTimestamps = ExtendIntArrayPool(curTimestamps);
                                curTimestamps[currentTimestampPosition++] = (curTimestamp + timeCalculationCache) * 1000;
                                timeCalculationCache = 0;
                                curStateStartPosition = i;
                                curTimestamp = 0;
                                state = CurrentState.PossiblyLyric;
                                continue;
                            default:
                                timeCalculationCache = timeCalculationCache * 10 + curChar - '0';
                                break;
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            ArrayPool<int>.Shared.Return(curTimestamps);
            lines.Sort();
            return lines.Cast<ILineInfo>().ToList();
        }

        private enum CurrentState
        {
            None,
            AwaitingState,
            AwaitingStateLyric,
            Attribute,
            AttributeContent,
            Timestamp,
            PossiblyLyric,
            Lyric
        }

        private enum TimeStampType
        {
            Minutes,
            Seconds,
            Milliseconds,
            None
        }

        private static int[] ExtendIntArrayPool(int[] oldArray)
        {
            int newSize = oldArray.Length * 2;
            int[] newArray = ArrayPool<int>.Shared.Rent(newSize);

            Array.Fill(newArray, -1);
            Array.Copy(oldArray, newArray, Math.Min(oldArray.Length, newSize));
            ArrayPool<int>.Shared.Return(oldArray);

            return newArray;
        }
    }
}
