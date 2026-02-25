namespace Lyricify.Lyrics.Searchers.Helpers
{
    public static partial class CompareHelper
    {
        /// <summary>
        /// 比较时长匹配程度
        /// </summary>
        /// <param name="name1">原曲目时长</param>
        /// <param name="name2">搜索得到的曲目时长</param>
        /// <returns>时长匹配程度</returns>
        public static DurationMatchType? CompareDuration(int? duration1, int? duration2)
        {
            if (duration1 == null || duration2 == null || duration1 == 0 || duration2 == 0) return null;

            return Math.Abs(duration1.Value - duration2.Value) switch
            {
                0 => DurationMatchType.Perfect,
                < 300 => DurationMatchType.VeryHigh,
                < 700 => DurationMatchType.High,
                < 1500 => DurationMatchType.Medium,
                < 3500 => DurationMatchType.Low,
                _ => DurationMatchType.NoMatch,
            };
        }

        public static int GetMatchScore(this DurationMatchType matchType)
        {
            return GetMatchScore(matchType);
        }

        public static int GetMatchScore(this DurationMatchType? matchType)
        {
            return matchType switch
            {
                DurationMatchType.Perfect => 7,
                DurationMatchType.VeryHigh => 6,
                DurationMatchType.High => 5,
                DurationMatchType.Medium => 4,
                DurationMatchType.Low => 2,
                DurationMatchType.NoMatch => 0,
                _ => 0,
            };
        }

        /// <summary>
        /// 时长匹配程度
        /// </summary>
        public enum DurationMatchType
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
