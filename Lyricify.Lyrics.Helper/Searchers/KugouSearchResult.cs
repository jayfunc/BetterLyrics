using Lyricify.Lyrics.Providers.Web.Kugou;
using Lyricify.Lyrics.Searchers.Helpers;

namespace Lyricify.Lyrics.Searchers
{
    public class KugouSearchResult : ISearchResult
    {
        public ISearcher Searcher => new KugouSearcher();

        public KugouSearchResult(string title, string[] artists, string album, string[]? albumArtists, int durationMs, string hash)
        {
            Title = title;
            Artists = artists;
            Album = album;
            AlbumArtists = albumArtists;
            DurationMs = durationMs;
            Hash = hash;
        }

        public KugouSearchResult(SearchSongResponse.DataItem.InfoItem song) : this(
            song.SongName,
            song.SingerName.Split('、'),
            song.AlbumName, // 很可能会包含中文译名
            null,
            song.Duration * 1000,
            song.Hash
            )
        { }

        public string Title { get; }

        public string[] Artists { get; }

        public string Album { get; }

        public string Hash { get; }

        public string[]? AlbumArtists { get; }

        public int? DurationMs { get; }

        public CompareHelper.MatchType? MatchType { get; set; }
    }
}
