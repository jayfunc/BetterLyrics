using Lyricify.Lyrics.Helpers.General;

namespace Lyricify.Lyrics.Searchers.Helpers
{
    public static partial class CompareHelper
    {
        /// <summary>
        /// 比较曲目名或专辑名匹配程度
        /// </summary>
        /// <param name="name1">原曲目名</param>
        /// <param name="name2">搜索得到的曲目名</param>
        /// <returns>名称匹配程度</returns>
        public static NameMatchType? CompareName(string? name1, string? name2)
        {
            if (name1 == null || name2 == null) return null;

            name1 = name1.ToSC(true).ToLower().Trim();
            name2 = name2.ToSC(true).ToLower().Trim();

            if (name1 == name2) return NameMatchType.Perfect;

            name1 = name1
                .Replace('’', '\'')
                .Replace('，', ',')
                .Replace("（", " (")
                .Replace("）", " )")
                .Replace('[', '(')
                .Replace(']', ')')
                .RemoveDuoSpaces();
            name2 = name2
                .Replace('’', '\'')
                .Replace('，', ',')
                .Replace("（", " (")
                .Replace("）", " )")
                .Replace('[', '(')
                .Replace(']', ')')
                .RemoveDuoSpaces();
            name1 = name1.Replace("acoustic version", "acoustic");
            name2 = name2.Replace("acoustic version", "acoustic");

            if ((name1.Replace(" - ", " (").Trim() + ")").Remove(" ")
                == (name2.Replace(" - ", " (").Trim() + ")").Remove(" "))
                return NameMatchType.VeryHigh;

            static bool SpecialCompare(string str1, string str2, string special)
            {
                special = "(" + special;
                bool c1 = str1.Contains(special);
                bool c2 = str2.Contains(special);
                if (c1 && !c2 && str1[..str1.IndexOf(special)].Trim() == str2) return true;
                if (c2 && !c1 && str2[..str2.IndexOf(special)].Trim() == str1) return true;
                return false;
            }

            static bool SingleSpecialCompare(string str1, string str2, string special)
            {
                special = "(" + special;
                if (str1.Contains(special) && str2.Contains(special)
                    && str1[..str1.IndexOf(special)].Trim() == str2[..str2.IndexOf(special)].Trim()) return true;
                return false;
            }

            static bool DuoSpecialCompare(string str1, string str2, string special1, string special2)
            {
                special1 = "(" + special1;
                special2 = "(" + special2;
                if (str1.Contains(special1) && str2.Contains(special2)
                    && str1[..str1.IndexOf(special1)].Trim() == str2[..str2.IndexOf(special2)].Trim()) return true;
                if (str1.Contains(special2) && str2.Contains(special1)
                    && str1[..str1.IndexOf(special2)].Trim() == str2[..str2.IndexOf(special1)].Trim()) return true;
                return false;
            }

            static bool BracketsCompare(string str1, string str2)
            {
                if (str1.Contains('(') && !str2.Contains('(')
                    && str1[..str1.IndexOf('(')].Trim() == str2) return true;
                if (str2.Contains('(') && !str2.Contains('(')
                    && str2[..str2.IndexOf('(')].Trim() == str1) return true;
                return false;
            }

            if (SpecialCompare(name1, name2, "deluxe")) return NameMatchType.VeryHigh;
            if (SpecialCompare(name1, name2, "explicit")) return NameMatchType.VeryHigh;
            if (SpecialCompare(name1, name2, "special edition")) return NameMatchType.VeryHigh;
            if (SpecialCompare(name1, name2, "bonus track")) return NameMatchType.VeryHigh;
            if (SpecialCompare(name1, name2, "feat")) return NameMatchType.VeryHigh;
            if (SpecialCompare(name1, name2, "with")) return NameMatchType.VeryHigh;

            if (DuoSpecialCompare(name1, name2, "feat", "explicit")) return NameMatchType.High;
            if (DuoSpecialCompare(name1, name2, "with", "explicit")) return NameMatchType.High;
            if (SingleSpecialCompare(name1, name2, "feat")) return NameMatchType.High;
            if (SingleSpecialCompare(name1, name2, "with")) return NameMatchType.High;

            if (BracketsCompare(name1, name2)) return NameMatchType.Medium;

            // 在同长度的情况下，判断解决异体字的问题
            int count = 0;
            if (name1.Length == name2.Length)
            {
                for (int i = 0; i < name1.Length; i++)
                    if (name1[i] == name2[i]) count++;

                if ((double)count / name1.Length >= 0.8 && name1.Length >= 4
                    || (double)count / name1.Length >= 0.5 && name1.Length >= 2 && name1.Length <= 3)
                    return NameMatchType.High;
            }

            if (StringHelper.ComputeTextSame(name1, name2, true) > 90) return NameMatchType.VeryHigh;
            if (StringHelper.ComputeTextSame(name1, name2, true) > 80) return NameMatchType.High;
            if (StringHelper.ComputeTextSame(name1, name2, true) > 68) return NameMatchType.Medium;
            if (StringHelper.ComputeTextSame(name1, name2, true) > 55) return NameMatchType.Low;

            return NameMatchType.NoMatch;
        }

        public static int GetMatchScore(this NameMatchType matchType)
        {
            return GetMatchScore(matchType);
        }

        public static int GetMatchScore(this NameMatchType? matchType)
        {
            return matchType switch
            {
                NameMatchType.Perfect => 7,
                NameMatchType.VeryHigh => 6,
                NameMatchType.High => 5,
                NameMatchType.Medium => 4,
                NameMatchType.Low => 2,
                NameMatchType.NoMatch => 0,
                _ => 0,
            };
        }

        /// <summary>
        /// 名称匹配程度
        /// </summary>
        public enum NameMatchType
        {
            Perfect,
            VeryHigh,
            High,
            Medium,
            Low,
            NoMatch = -1,
        }
    }
}
