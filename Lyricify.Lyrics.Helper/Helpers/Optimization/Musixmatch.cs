using Lyricify.Lyrics.Models;

namespace Lyricify.Lyrics.Helpers.Optimization
{
    public static class Musixmatch
    {
        /// <summary>
        /// 针对 Musixmatch 逐音节歌词格式的优化 (合并空格)
        /// </summary>
        public static void StandardizeMusixmatchLyrics(List<ILineInfo> list)
        {
            foreach (ILineInfo line in list)
            {
                if (line is SyllableLineInfo syllableLine)
                {
                    StandardizeMusixmatchLyrics(syllableLine);
                }
            }
        }

        /// <summary>
        /// 针对 Musixmatch 逐音节歌词格式的优化 (合并空格)
        /// <br />注意：此方法无法处理经过逐音节合并后的单词，会抛出 InvalidCastException 异常。
        /// </summary>
        public static void StandardizeMusixmatchLyrics(SyllableLineInfo syllableLine)
        {
            for (int i = 1; i < syllableLine.Syllables.Count; i++)
            {
                if (syllableLine.Syllables[i].Text == " ")
                {
                    ((SyllableInfo)syllableLine.Syllables[i - 1]).Text += " ";
                    syllableLine.Syllables.RemoveAt(i--);
                }
            }
        }
    }
}
