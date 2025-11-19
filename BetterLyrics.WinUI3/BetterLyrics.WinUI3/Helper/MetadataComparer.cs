using BetterLyrics.WinUI3.Models;
using F23.StringSimilarity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BetterLyrics.WinUI3.Helper
{
    public static class MetadataComparer
    {
        // 权重配置 (总和 1.0)
        private const double WeightTitle = 0.40;
        private const double WeightArtist = 0.40;
        private const double WeightAlbum = 0.10;
        private const double WeightDuration = 0.10;

        // 实例化算法 (JaroWinkler 适合短字符串匹配)
        private static readonly JaroWinkler _algo = new JaroWinkler();

        /// <summary>
        /// 计算 SongInfo 和 LyricsSearchResult 的相似度 (0-100)
        /// </summary>
        public static int CalculateScore(SongInfo local, LyricsSearchResult remote)
        {
            if (local == null || remote == null) return 0;

            // 1. 标题相似度
            double titleScore = GetStringSimilarity(local.Title, remote.Title);

            // 2. 艺术家相似度 (需要处理数组顺序)
            double artistScore = GetArtistSimilarity(local.Artists, remote.Artists);

            // 3. 专辑相似度
            double albumScore = GetStringSimilarity(local.Album, remote.Album);

            // 4. 时长相似度 (基于毫秒 vs 秒的转换和容差)
            double durationScore = GetDurationSimilarity(local.DurationMs, remote.Duration);

            // 5. 加权汇总
            double totalScore = (titleScore * WeightTitle) +
                                (artistScore * WeightArtist) +
                                (albumScore * WeightAlbum) +
                                (durationScore * WeightDuration);

            return (int)Math.Round(totalScore * 100);
        }

        private static double GetStringSimilarity(string? s1, string? s2)
        {
            // 归一化：转小写，去空白
            s1 = s1?.Trim().ToLowerInvariant() ?? "";
            s2 = s2?.Trim().ToLowerInvariant() ?? "";

            if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2)) return 1.0; // 都是空，视为匹配
            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0.0; // 其中一个为空

            return _algo.Similarity(s1, s2);
        }

        private static double GetArtistSimilarity(string[]? localArtists, string[]? remoteArtists)
        {
            if (localArtists == null || localArtists.Length == 0) return 0.0;
            if (remoteArtists == null || remoteArtists.Length == 0) return 0.0;

            // 技巧：将艺术家数组排序并连接，避免顺序不同导致的不匹配
            // 例如: ["Jay-Z", "Linkin Park"] 和 ["Linkin Park", "Jay-Z"] 应该是一样的
            var s1 = string.Join(" ", localArtists.OrderBy(a => a).Select(a => a.Trim().ToLowerInvariant()));
            var s2 = string.Join(" ", remoteArtists.OrderBy(a => a).Select(a => a.Trim().ToLowerInvariant()));

            return _algo.Similarity(s1, s2);
        }

        private static double GetDurationSimilarity(double localMs, double? remoteSeconds)
        {
            if (remoteSeconds == null || remoteSeconds == 0) return 0.0; // 远程没有时长数据，不匹配

            double localSeconds = localMs / 1000.0;
            double diff = Math.Abs(localSeconds - remoteSeconds.Value);

            // 容差逻辑：
            // 差距 <= 3秒：100% 相似
            // 差距 >= 20秒：0% 相似
            // 中间线性插值

            const double PerfectTolerance = 3.0;
            const double MaxTolerance = 20.0;

            if (diff <= PerfectTolerance) return 1.0;
            if (diff >= MaxTolerance) return 0.0;

            // 线性递减公式
            return 1.0 - ((diff - PerfectTolerance) / (MaxTolerance - PerfectTolerance));
        }
    }
}
