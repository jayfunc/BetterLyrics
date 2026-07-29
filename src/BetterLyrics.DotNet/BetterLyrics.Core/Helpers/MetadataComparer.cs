using System.Text.RegularExpressions;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Entities;
using F23.StringSimilarity;

namespace BetterLyrics.Core.Helpers;

public static partial class MetadataComparer
{
    private const double WeightTitle = 0.30;
    private const double WeightArtist = 0.30;
    private const double WeightAlbum = 0.10;
    private const double WeightDuration = 0.30;

    // JaroWinkler 适合短字符串匹配
    private static readonly JaroWinkler _algo = new();

    public static int CalculateScore(SongInfo songInfo, LyricsCacheItem remote)
    {
        return CalculateScore(songInfo, remote.Title, remote.Artist, remote.Album, remote.Duration);
    }

    public static int CalculateScore(SongInfo songInfo, FilesIndexItem local)
    {
        return CalculateScore(songInfo, local.Title, local.Artists, local.Album, local.Duration, local.FileName);
    }

    public static int CalculateScore(
        SongInfo songInfo,
        string? compareTitle, string? compareArtist, string? compareAlbum, double? compareDuration,
        string? compareFileName = null)
    {
        double totalScore = 0;

        var localHasMetadata = !string.IsNullOrWhiteSpace(songInfo.Title);
        var remoteHasMetadata = !string.IsNullOrWhiteSpace(compareTitle);

        if (localHasMetadata && remoteHasMetadata)
        {
            var titleScore = GetStringSimilarity(songInfo.Title, compareTitle);
            var artistScore = GetStringSimilarity(songInfo.Artist, compareArtist);
            var albumScore = GetStringSimilarity(songInfo.Album, compareAlbum);
            var durationScore = GetDurationSimilarity(songInfo.Duration, compareDuration);

            totalScore = titleScore * WeightTitle +
                         artistScore * WeightArtist +
                         albumScore * WeightAlbum +
                         durationScore * WeightDuration;
        }
        else
        {
            var localQuery = localHasMetadata
                ? $"{songInfo.Title} {songInfo.Artist}"
                : Path.GetFileNameWithoutExtension(songInfo.LinkedFileName);

            var remoteQuery = remoteHasMetadata
                ? $"{compareTitle} {compareArtist}"
                : Path.GetFileNameWithoutExtension(compareFileName);

            var fp1 = CreateSortedFingerprint(localQuery);
            var fp2 = CreateSortedFingerprint(remoteQuery);

            if (string.IsNullOrWhiteSpace(fp1) || string.IsNullOrWhiteSpace(fp2))
                totalScore = 0;
            else
                totalScore = _algo.Similarity(fp1, fp2);
        }

        return (int)Math.Round(totalScore * 100);
    }

    public static int CalculateScore(
        SongInfo songInfo,
        string[] compareTitles, string[] compareArtists, string[] compareAlbums, double? compareDuration,
        string? compareFileName = null)
    {
        double totalScore = 0;

        var localHasMetadata = !string.IsNullOrWhiteSpace(songInfo.Title);
        var remoteHasMetadata = compareTitles != null && compareTitles.Length > 0 && !string.IsNullOrWhiteSpace(compareTitles[0]);

        if (localHasMetadata && remoteHasMetadata)
        {
            var titleScore = compareTitles?.Max(t => GetStringSimilarity(songInfo.Title, t)) ?? 0;
            var artistScore = compareArtists?.Max(a => GetStringSimilarity(songInfo.Artist, a)) ?? 0;
            var albumScore = compareAlbums?.Max(a => GetStringSimilarity(songInfo.Album, a)) ?? 0;
            var durationScore = GetDurationSimilarity(songInfo.Duration, compareDuration);

            totalScore = titleScore * WeightTitle +
                         artistScore * WeightArtist +
                         albumScore * WeightAlbum +
                         durationScore * WeightDuration;
        }
        else
        {
            var localQuery = localHasMetadata
                ? $"{songInfo.Title} {songInfo.Artist}"
                : Path.GetFileNameWithoutExtension(songInfo.LinkedFileName);

            var bestRemoteScore = 0.0;
            if (compareTitles != null && compareArtists != null)
            {
                var fp1 = CreateSortedFingerprint(localQuery);
                if (!string.IsNullOrWhiteSpace(fp1))
                {
                    foreach (var t in compareTitles)
                    {
                        foreach (var a in compareArtists)
                        {
                            var remoteQuery = $"{t} {a}";
                            var fp2 = CreateSortedFingerprint(remoteQuery);
                            if (!string.IsNullOrWhiteSpace(fp2))
                            {
                                var score = _algo.Similarity(fp1, fp2);
                                if (score > bestRemoteScore) bestRemoteScore = score;
                            }
                        }
                    }
                }
            }

            totalScore = bestRemoteScore;
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

    private static double GetDurationSimilarity(double localSeconds, double? remoteSeconds)
    {
        if (remoteSeconds == null || remoteSeconds == 0) return 0.0; // 远程没有时长数据，不匹配

        var diff = Math.Abs(localSeconds - remoteSeconds.Value);

        // 差距 <= 1 秒：100 % 相似
        // 差距 >= 10 秒：0 % 相似
        // 中间线性插值

        const double PerfectTolerance = 1.0;
        const double MaxTolerance = 10.0;

        if (diff <= PerfectTolerance) return 1.0;
        if (diff >= MaxTolerance) return 0.0;

        return 1.0 - (diff - PerfectTolerance) / (MaxTolerance - PerfectTolerance);
    }

    private static string CreateSortedFingerprint(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "";

        input = input.ToLowerInvariant();

        var cleaned = NonWordCharactersRegex().Replace(input, " ");

        var tokens = cleaned.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .OrderBy(t => t); // 排序

        return string.Join(" ", tokens);
    }

    [GeneratedRegex(@"[\p{P}\p{S}]")]
    private static partial Regex NonWordCharactersRegex();
}