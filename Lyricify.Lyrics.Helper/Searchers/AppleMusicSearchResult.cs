using Lyricify.Lyrics.Providers.Web.AppleMusic;
using Lyricify.Lyrics.Searchers.Helpers;

namespace Lyricify.Lyrics.Searchers
{
    public class AppleMusicSearchResult : ISearchResult
    {
        public ISearcher Searcher => new AppleMusicSearcher();

        public AppleMusicSearchResult(string title, string[] artists, string album, string[]? albumArtists, int durationMs, string id)
        {
            Title = title;
            Artists = artists;
            Album = album;
            AlbumArtists = albumArtists;
            DurationMs = durationMs;
            Id = id;
        }

        public AppleMusicSearchResult(SongData song) : this(
             song.Attributes?.Name ?? string.Empty,
             string.IsNullOrWhiteSpace(song.Attributes?.ArtistName)
                 ? Array.Empty<string>()
                 : SplitArtists(song.Attributes!.ArtistName!),
             song.Attributes?.AlbumName ?? string.Empty,
             null,
             song.Attributes?.DurationInMillis ?? 0,
             song.Id ?? string.Empty
        )
        { }

        private static string[] SplitArtists(string artistName)
        {
            // 按 ", " 全量拆分
            var parts = artistName
                .Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .ToList();

            if (parts.Count == 0)
                return Array.Empty<string>();

            // 处理 " & "
            var last = parts[^1];
            var ampIndex = last.LastIndexOf(" & ", StringComparison.Ordinal);

            if (ampIndex >= 0)
            {
                parts.RemoveAt(parts.Count - 1);

                var left = last[..ampIndex].Trim();
                var right = last[(ampIndex + 3)..].Trim();

                if (!string.IsNullOrEmpty(left))
                    parts.Add(left);
                if (!string.IsNullOrEmpty(right))
                    parts.Add(right);
            }

            return parts.ToArray();
        }

        public string Title { get; }

        public string[] Artists { get; }

        public string Album { get; }

        public string Id { get; }

        public string[]? AlbumArtists { get; }

        public int? DurationMs { get; }

        public CompareHelper.MatchType? MatchType { get; set; }
    }
}
