using Lyricify.Lyrics.Helpers.General;
using Lyricify.Lyrics.Models;
using System.Text;

namespace Lyricify.Lyrics.Generators
{
    public static class LrcGenerator
    {
        /// <summary>
        /// 生成 LRC 字符串
        /// </summary>
        /// <param name="lyricsData">用于生成的源歌词数据</param>
        /// <param name="endTimeOutputType">作为行末时间的空行的输出类型</param>
        /// <param name="subLinesOutputType">子行的输出方式</param>
        /// <returns>生成出的 LRC 字符串</returns>
        public static string Generate(LyricsData lyricsData, EndTimeOutputType endTimeOutputType = EndTimeOutputType.Huge, SubLinesOutputType subLinesOutputType = SubLinesOutputType.InMainLine)
        {
            if (lyricsData?.Lines is not { Count: > 0 }) return string.Empty;

            var sb = new StringBuilder();
            var lines = lyricsData.Lines;

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (subLinesOutputType == SubLinesOutputType.InDiffLine)
                {
                    AppendLine(line);
                    if (ShouldAddLine(line, false, i))
                        AppendEmptyLine(line.EndTime!.Value);

                    if (line.SubLine is not null)
                    {
                        AppendLine(line.SubLine);
                        if (ShouldAddLine(line.SubLine, false, i))
                            AppendEmptyLine(line.SubLine.EndTime!.Value);
                    }
                }
                else
                {
                    AppendLineWithSub(line);
                    if (ShouldAddLine(line, true, i))
                        AppendEmptyLine(line.EndTimeWithSubLine!.Value);
                }

            }

            return sb.ToString();

            void AppendLine(ILineInfo line)
            {
                if (!line.StartTime.HasValue)
                    return;
                sb.AppendLine($"[{StringHelper.FormatTimeMsToTimestampString(line.StartTime.Value)}]{line.Text}");
            }

            void AppendLineWithSub(ILineInfo line)
            {
                if (!line.StartTimeWithSubLine.HasValue)
                    return;
                sb.AppendLine($"[{StringHelper.FormatTimeMsToTimestampString(line.StartTimeWithSubLine.Value)}]{line.FullText}");
            }

            void AppendEmptyLine(int timeStamp)
            {
                sb.AppendLine($"[{StringHelper.FormatTimeMsToTimestampString(timeStamp)}]");
            }

            bool ShouldAddLine(ILineInfo line, bool withSub, int index)
            {
                var endTime = withSub ? line.EndTimeWithSubLine : line.EndTime;
                if (lines is null || endTime.HasValue == false)
                    return false;
                if (line.EndTime <= 0)
                    return false;
                if (endTimeOutputType == EndTimeOutputType.All)
                    return true;
                if (endTimeOutputType == EndTimeOutputType.Huge)
                {
                    if (index + 1 >= lines.Count)
                        return true;
                    if (lines[index + 1].StartTimeWithSubLine - endTime > 5000)
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 作为行末时间的空行的输出类型
        /// </summary>
        public enum EndTimeOutputType
        {
            /// <summary>
            /// 不输出作为行末时间的空行
            /// </summary>
            None,

            /// <summary>
            /// 输出间距 (5s 以上) 较大的行末时间空行
            /// </summary>
            Huge,

            /// <summary>
            /// 输出所有行末时间空行
            /// </summary>
            All,
        }

        /// <summary>
        /// 子行的输出方式
        /// </summary>
        public enum SubLinesOutputType
        {
            /// <summary>
            /// 通过括号嵌在主行中
            /// </summary>
            InMainLine,

            /// <summary>
            /// 子行单独成行
            /// </summary>
            InDiffLine,
        }
    }
}
