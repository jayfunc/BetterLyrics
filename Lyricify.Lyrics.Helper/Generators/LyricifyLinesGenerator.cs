using Lyricify.Lyrics.Models;
using System.Text;

namespace Lyricify.Lyrics.Generators
{
    public static class LyricifyLinesGenerator
    {
        /// <summary>
        /// 生成 Lyricify Lines 字符串
        /// </summary>
        /// <param name="lyricsData">用于生成的源歌词数据</param>
        /// <param name="subLinesOutputType">子行的输出方式</param>
        /// <returns>生成出的 Lyricify Lines 字符串</returns>
        public static string Generate(LyricsData lyricsData, SubLinesOutputType subLinesOutputType = SubLinesOutputType.InMainLine)
        {
            if (lyricsData?.Lines is not { Count: > 0 }) return string.Empty;

            var sb = new StringBuilder();
            var lines = lyricsData.Lines;
            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];
                if (subLinesOutputType == SubLinesOutputType.InDiffLine)
                {
                    AppendLine(sb, line);
                    if (line.SubLine is not null)
                        AppendLine(sb, line.SubLine);
                }
                else
                {
                    if (line.StartTimeWithSubLine.HasValue)
                        sb.AppendLine($"[{line.StartTimeWithSubLine},{line.EndTimeWithSubLine ?? 0}]{line.FullText}");
                }

            }

            return sb.ToString();

            static void AppendLine(StringBuilder sb, ILineInfo line)
            {
                if (line.StartTime.HasValue)
                    sb.AppendLine($"[{line.StartTime},{line.EndTime ?? 0}]{line.Text}");
            }
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
