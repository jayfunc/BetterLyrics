using Lyricify.Lyrics.Models;
using System.Text;

namespace Lyricify.Lyrics.Generators
{
    public static class YrcGenerator
    {
        /// <summary>
        /// 生成 YRC 字符串
        /// </summary>
        /// <param name="lyricsData">用于生成的源歌词数据</param>
        /// <returns>生成出的 YRC 字符串</returns>
        public static string Generate(LyricsData lyricsData)
        {
            if (lyricsData?.Lines is not { Count: > 0 }) return string.Empty;

            var sb = new StringBuilder();
            var lines = lyricsData.Lines;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i] is SyllableLineInfo line)
                {
                    AppendLine(sb, line);
                    if (line.SubLine is SyllableLineInfo subLine)
                        AppendLine(sb, subLine, true);
                }
            }

            return sb.ToString();

            static void AppendLine(StringBuilder sb, SyllableLineInfo line, bool isSubLine = false)
            {
                // 添加行信息
                sb.Append('[');
                sb.Append(line.StartTime);
                sb.Append(',');
                sb.Append(((ILineInfo)line).Duration);
                sb.Append(']');

                // 添加音节信息
                foreach (var syllable in line.Syllables)
                {
                    if (syllable is SyllableInfo syllableInfo)
                    {
                        Append(syllableInfo);
                    }
                    else if (syllable is FullSyllableInfo fullSyllableInfo)
                    {
                        foreach (var item in fullSyllableInfo.SubItems)
                        {
                            Append(item);
                        }
                    }
                }
                sb.AppendLine();

                void Append(SyllableInfo item)
                {
                    sb.Append('(');
                    sb.Append(item.StartTime);
                    sb.Append(',');
                    sb.Append(((ISyllableInfo)item).Duration);
                    sb.Append(",0)");
                    sb.Append(item.Text);
                }
            }
        }
    }
}
