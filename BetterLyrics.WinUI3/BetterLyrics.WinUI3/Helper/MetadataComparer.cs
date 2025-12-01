using BetterLyrics.WinUI3.Models;
using F23.StringSimilarity;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace BetterLyrics.WinUI3.Helper
{
    public static partial class MetadataComparer
    {
        private const double WeightTitle = 0.40;
        private const double WeightArtist = 0.40;
        private const double WeightAlbum = 0.10;
        private const double WeightDuration = 0.10;

        // JaroWinkler 适合短字符串匹配
        private static readonly JaroWinkler _algo = new();

        public static int CalculateScore(SongInfo local, LyricsSearchResult remote)
        {
            if (local == null || remote == null) return 0;

            double totalScore = 0;

            bool localHasMetadata = !string.IsNullOrWhiteSpace(local.Title);
            bool remoteHasMetadata = !string.IsNullOrWhiteSpace(remote.Title);

            if (localHasMetadata && remoteHasMetadata)
            {
                double titleScore = GetStringSimilarity(local.Title, remote.Title);
                double artistScore = GetArtistSimilarity(local.Artists, remote.Artists);
                double albumScore = GetStringSimilarity(local.Album, remote.Album);
                double durationScore = GetDurationSimilarity(local.DurationMs, remote.Duration);

                totalScore = (titleScore * WeightTitle) +
                                    (artistScore * WeightArtist) +
                                    (albumScore * WeightAlbum) +
                                    (durationScore * WeightDuration);
            }
            else
            {
                string? localQuery = localHasMetadata
                    ? $"{local.Title} {string.Join(" ", local.Artists ?? [])}"
                    : Path.GetFileNameWithoutExtension(local.LinkedFileName);

                string remoteQuery = remoteHasMetadata
                    ? $"{remote.Title} {string.Join(" ", remote.Artists ?? [])}"
                    : Path.GetFileNameWithoutExtension(remote.Reference);

                string fp1 = CreateSortedFingerprint(localQuery);
                string fp2 = CreateSortedFingerprint(remoteQuery);

                if (string.IsNullOrWhiteSpace(fp1) || string.IsNullOrWhiteSpace(fp2))
                    totalScore = 0;
                else
                    totalScore = _algo.Similarity(fp1, fp2);
            }

            return (int)Math.Round(totalScore * 100);
        }

        private static double GetStringSimilarity(string? s1, string? s2)
        {
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

            // 将艺术家数组排序并连接，避免顺序不同导致的不匹配
            var s1 = string.Join(" ", localArtists.OrderBy(a => a).Select(a => a.Trim().ToLowerInvariant()));
            var s2 = string.Join(" ", remoteArtists.OrderBy(a => a).Select(a => a.Trim().ToLowerInvariant()));

            return _algo.Similarity(s1, s2);
        }

        private static double GetDurationSimilarity(double localMs, double? remoteSeconds)
        {
            if (remoteSeconds == null || remoteSeconds == 0) return 0.0; // 远程没有时长数据，不匹配

            double localSeconds = localMs / 1000.0;
            double diff = Math.Abs(localSeconds - remoteSeconds.Value);

            // 差距 <= 3秒：100% 相似
            // 差距 >= 20秒：0% 相似
            // 中间线性插值

            const double PerfectTolerance = 3.0;
            const double MaxTolerance = 20.0;

            if (diff <= PerfectTolerance) return 1.0;
            if (diff >= MaxTolerance) return 0.0;

            return 1.0 - ((diff - PerfectTolerance) / (MaxTolerance - PerfectTolerance));
        }

        private static string CreateSortedFingerprint(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "";

            input = input.ToLowerInvariant();

            string cleaned = NonWordCharactersRegex().Replace(input, " ");

            var tokens = cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                .OrderBy(t => t); // 排序

            return string.Join(" ", tokens);
        }

        [GeneratedRegex(@"[\p{P}\p{S}]")]
        private static partial Regex NonWordCharactersRegex();
    }
}
