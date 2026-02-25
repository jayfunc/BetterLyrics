using Lyricify.Lyrics.Models;

namespace Lyricify.Lyrics.Helpers
{
    public static class OffsetHelper
    {
        public static void AddOffset(List<ILineInfo> lines, int offset)
        {
            foreach (var line in lines)
            {
                AddOffset(line, offset);
            }
        }

        public static void AddOffset(ILineInfo lines, int offset)
        {
            if (lines is LineInfo lineInfo)
            {
                AddOffset(lineInfo, offset);
            }
            else if (lines is SyllableLineInfo syllableLineInfo)
            {
                AddOffset(syllableLineInfo, offset);
            }
        }

        public static void AddOffset(LineInfo line, int offset)
        {
            line.StartTime -= offset;
            line.EndTime -= offset;
        }

        public static void AddOffset(SyllableLineInfo line, int offset)
        {
            var syllables = line.Syllables;
            foreach (var syllable in syllables)
            {
                if (syllable is SyllableInfo syllableInfo)
                {
                    syllableInfo.StartTime -= offset;
                    syllableInfo.EndTime -= offset;
                }
                else if (syllable is FullSyllableInfo fullSyllableInfo)
                {
                    foreach (var subItems in fullSyllableInfo.SubItems)
                    {
                        subItems.StartTime -= offset;
                        subItems.EndTime -= offset;
                    }
                }
            }
        }
    }
}
