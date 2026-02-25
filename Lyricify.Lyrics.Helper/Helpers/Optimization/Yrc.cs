using Lyricify.Lyrics.Models;

namespace Lyricify.Lyrics.Helpers.Optimization
{
    public static class Yrc
    {
        /// <summary>
        /// 针对 YRC 歌词格式的优化
        /// </summary>
        public static void StandardizeYrcLyrics(List<ILineInfo> list)
        {
            foreach (ILineInfo line in list)
            {
                if (line is SyllableLineInfo syllableLine)
                {
                    StandardizeYrcLyrics(syllableLine);
                }
            }
        }

        /// <summary>
        /// 针对 YRC 歌词格式的优化
        /// <br />注意：此方法无法处理经过逐音节合并后的单词，会抛出 InvalidCastException 异常。
        /// </summary>
        public static void StandardizeYrcLyrics(SyllableLineInfo line)
        {
            var list = line.Syllables;

            // 移除最后的空格
            while (list.Last().Text == " ")
            {
                list.RemoveAt(list.Count - 1);
            }

            for (int i = 0; i < list.Count; i++)
            {
                // 移除空白格
                if (list[i].Text.Length == 0)
                {
                    list.RemoveAt(i);
                    i--; continue;
                }

                // 合并单独的空格
                if (list[i].Text == " ")
                {
                    if (i - 1 >= 0) ((SyllableInfo)list[i - 1]).Text += list[i].Text;

                    list.RemoveAt(i);
                    i--; continue;
                }

                // 合并标点符号
                if (i > 0 && list[i].Text.Length <= 2 &&
                    (list[i].Text[0] == ',' || list[i].Text[0] == '.'
                    || list[i].Text[0] == '?' || list[i].Text[0] == '!'
                    || list[i].Text[0] == '\"'))
                {
                    if (i - 1 >= 0) ((SyllableInfo)list[i - 1]).Text += list[i].Text;

                    list.RemoveAt(i);
                    i--; continue;
                }
            }
        }
    }
}
