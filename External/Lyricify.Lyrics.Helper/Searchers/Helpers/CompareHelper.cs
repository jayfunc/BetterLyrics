using Lyricify.Lyrics.Models;

namespace Lyricify.Lyrics.Searchers.Helpers
{
    public static partial class CompareHelper
    {
        /// <summary>
        /// 比较曲目匹配程度
        /// </summary>
        /// <param name="track">原曲目</param>
        /// <param name="searchResult">搜索得到的曲目</param>
        /// <returns>曲目匹配程度</returns>
        public static MatchType CompareTrack(ITrackMetadata track, ISearchResult searchResult)
        {
            return CompareTrack(TrackMultiArtistMetadata.GetTrackMultiArtistMetadata(track), searchResult);
        }

        /// <summary>
        /// 比较曲目匹配程度
        /// </summary>
        /// <param name="track">原曲目</param>
        /// <param name="searchResult">搜索得到的曲目</param>
        /// <returns>曲目匹配程度</returns>
        public static MatchType CompareTrack(TrackMultiArtistMetadata track, ISearchResult searchResult)
        {
            var trackMatch = CompareName(track.Title, searchResult.Title);
            var artistMatch = CompareArtist(track.Artists, searchResult.Artists);
            var albumMatch = CompareName(track.Album, searchResult.Album);
            var albumArtistMatch = CompareArtist(track.AlbumArtists, searchResult.AlbumArtists);
            var durationMatch = CompareDuration(track.DurationMs, searchResult.DurationMs);

            var totalScore = 0d;
            totalScore += trackMatch.GetMatchScore();
            totalScore += artistMatch.GetMatchScore();
            totalScore += albumMatch.GetMatchScore() * 0.4;
            totalScore += albumArtistMatch.GetMatchScore() * 0.2;
            totalScore += durationMatch.GetMatchScore();

            // 针对 MatchType 为 null 的进行按比例拉伸调整
            var nullCount = 0d;
            const int fullScore = 30; // 25.2
            nullCount += albumMatch is null ? 0.4 : 0;
            nullCount += albumArtistMatch is null ? 0.2 : 0;
            nullCount += durationMatch is null ? 1 : 0;
            totalScore = totalScore * fullScore / (fullScore - nullCount * 7);

            return totalScore switch
            {
                > 21 => MatchType.Perfect,
                > 19 => MatchType.VeryHigh,
                > 17 => MatchType.High,
                > 15 => MatchType.PrettyHigh,
                > 11 => MatchType.Medium,
                > 8 => MatchType.Low,
                > 3 => MatchType.VeryLow,
                _ => MatchType.NoMatch,
            };
        }

        /// <summary>
        /// 曲目匹配程度
        /// </summary>
        public enum MatchType
        {
            Perfect = 100,
            VeryHigh = 99,
            High = 95,
            PrettyHigh = 90,
            Medium = 70,
            Low = 30,
            VeryLow = 10,
            NoMatch = -1,
        }
    }

    public class MatchTypeComparer : IComparer<CompareHelper.MatchType>
    {
        public int Compare(CompareHelper.MatchType x, CompareHelper.MatchType y)
        {
            return ((int)x).CompareTo((int)y);
        }
    }
}
