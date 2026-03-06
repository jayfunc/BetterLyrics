namespace Lyricify.Lyrics.Searchers
{
    public class AppleMusicSearcher : Searcher, ISearcher
    {
        public override string Name => "AppleMusic";

        public override string DisplayName => "Apple Music";

        public override Searchers SearcherType => Searchers.AppleMusic;

        public override async Task<List<ISearchResult>?> SearchForResults(string searchString)
        {
            try
            {
                var result = await Providers.Web.Providers.AppleMusicApi.Search(searchString, limit: 20);
                var songs = result?.Results?.Songs?.Data;

                if (songs == null || songs.Length == 0) return null;

                var list = new List<ISearchResult>(songs.Length);
                foreach (var song in songs)
                {
                    list.Add(new AppleMusicSearchResult(song));
                }

                return list;
            }
            catch
            {
                return null;
            }
        }
    }
}
