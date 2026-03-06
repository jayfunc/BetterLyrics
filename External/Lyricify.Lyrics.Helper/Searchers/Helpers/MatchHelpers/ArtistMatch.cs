using Lyricify.Lyrics.Helpers.General;

namespace Lyricify.Lyrics.Searchers.Helpers
{
    public static partial class CompareHelper
    {
        /// <summary>
        /// 比较艺人匹配程度
        /// </summary>
        /// <param name="artist1">原曲目的艺人</param>
        /// <param name="artist2">搜索得到的曲目的艺人</param>
        /// <returns>艺人匹配程度</returns>
        public static ArtistMatchType? CompareArtist(IEnumerable<string>? artist1, IEnumerable<string>? artist2)
        {
            if (artist1 == null || artist2 == null) return null;

            var list1 = artist1.ToList();
            var list2 = artist2.ToList();

            // 预处理：转小写 & 中文转换
            for (int i = 0; i < list1.Count; i++)
                list1[i] = list1[i].ToLower().ToSC(true);
            for (int i = 0; i < list2.Count; i++)
                list2[i] = list2[i].ToLower().ToSC(true);

            // 比较匹配数量
            int count = 0;
            foreach (var art in list2)
                if (list1.Contains(art)) count++;

            if (count == list1.Count && list1.Count == list2.Count)
                return ArtistMatchType.Perfect;

            if (count + 1 >= list1.Count && list1.Count >= 2 || list1.Count > 6 && (double)count / list1.Count > 0.8)
                return ArtistMatchType.VeryHigh;

            if (count == 1 && list1.Count == 1 && list2.Count == 2)
                return ArtistMatchType.High;

            if (list1.Count > 5 && (list2[0].Contains("Various") || list2[0].Contains("群星")))
                return ArtistMatchType.VeryHigh;

            if (list1.Count > 7 && list2.Count > 7 && (double)count / list1.Count > 0.66)
                return ArtistMatchType.High;

            if (list1.Count == 1 && list2.Count > 1 && list1[0].StartsWith(list2[0]))
                return ArtistMatchType.High;

            if (list1.Count == 1 && list2.Count > 1 && list2[0].Length > 3 && list1[0].Contains(list2[0]))
                return ArtistMatchType.High;

            if (list1.Count == 1 && list2.Count > 1 && list2[0].Length > 1 && list1[0].Contains(list2[0]))
                return ArtistMatchType.Medium;

            if (count == 1 && list1.Count == 1 && list2.Count >= 3)
                return ArtistMatchType.Medium;

            if (count >= 2)
                return ArtistMatchType.Low;

            return ArtistMatchType.NoMatch;
        }

        public static int GetMatchScore(this ArtistMatchType matchType)
        {
            return GetMatchScore(matchType);
        }

        public static int GetMatchScore(this ArtistMatchType? matchType)
        {
            return matchType switch
            {
                ArtistMatchType.Perfect => 7,
                ArtistMatchType.VeryHigh => 6,
                ArtistMatchType.High => 5,
                ArtistMatchType.Medium => 4,
                ArtistMatchType.Low => 2,
                ArtistMatchType.NoMatch => 0,
                _ => 0,
            };
        }

        /// <summary>
        /// 艺人匹配程度
        /// </summary>
        public enum ArtistMatchType
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
