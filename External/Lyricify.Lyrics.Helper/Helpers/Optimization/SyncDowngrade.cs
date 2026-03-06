using Lyricify.Lyrics.Models;

namespace Lyricify.Lyrics.Helpers.Optimization
{
    public static class SyncDowngrade
    {
        public static void DowngradeToLineSynced(this List<ILineInfo> list)
        {
            if (list == null || list.Count == 0) return;

            for (int i = 0; i < list.Count; i++)
            {
                list[i] = DowngradeToLineSynced(list[i]);
            }
        }

        public static ILineInfo DowngradeToLineSynced(this ILineInfo line)
        {
            if (line == null) return line!;

            // 先递归处理 SubLine
            ILineInfo? sub = line.SubLine;
            if (sub != null)
                sub = DowngradeToLineSynced(sub);

            // 1) 已经是 LineInfo / FullLineInfo：本身就是“行级”，不需要降级，只更新 SubLine
            if (line is LineInfo lineAlready)
            {
                lineAlready.SubLine = sub;
                return lineAlready;
            }

            // 2) 逐字/逐音节：SyllableLineInfo / FullSyllableLineInfo => 降级为 LineInfo / FullLineInfo
            if (line is SyllableLineInfo syllableLine)
            {
                var syllables = syllableLine.Syllables;
                if (syllables == null || syllables.Count == 0)
                {
                    // 没有 syllable 的异常情况：只更新 SubLine
                    syllableLine.SubLine = sub;
                    return syllableLine;
                }

                var text = syllableLine.Text;
                var start = syllables.First().StartTime;
                var end = syllables.Last().EndTime;

                // 如果带翻译/注音信息：生成 FullLineInfo 并复制字典 & 注音
                if (line is IFullLineInfo full)
                {
                    var downgradedFull = new FullLineInfo
                    {
                        Text = text,
                        StartTime = start,
                        EndTime = end,
                        LyricsAlignment = syllableLine.LyricsAlignment,
                        SubLine = sub,
                        Pronunciation = full.Pronunciation,
                        Translations = full.Translations != null
                            ? new Dictionary<string, string>(full.Translations)
                            : new Dictionary<string, string>()
                    };

                    return downgradedFull;
                }

                // 普通降级：生成 LineInfo
                var downgraded = new LineInfo
                {
                    Text = text,
                    StartTime = start,
                    EndTime = end,
                    LyricsAlignment = syllableLine.LyricsAlignment,
                    SubLine = sub
                };

                return downgraded;
            }

            // 3) 其它未知实现：不强行处理，只更新 SubLine（若可写则写回）
            return line;
        }
    }
}
