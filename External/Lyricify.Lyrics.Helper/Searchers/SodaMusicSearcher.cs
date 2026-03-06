namespace Lyricify.Lyrics.Searchers
{
    public class SodaMusicSearcher : Searcher, ISearcher
    {
        public override string Name => "SodaMusic";

        public override string DisplayName => "Soda Music";

        public override Searchers SearcherType => Searchers.SodaMusic;

        public override async Task<List<ISearchResult>?> SearchForResults(string searchString)
        {
            var search = new List<ISearchResult>();

            try
            {
                var result = await Providers.Web.Providers.SodaMusicApi.Search(searchString);
                var resultDataList = result?.ResultGroups[0]?.Data;
                if (resultDataList == null) return null;
                foreach (var resultData in resultDataList)
                {
                    if (resultData.Meta?.ItemType != "track") continue;
                    search.Add(new SodaMusicSearchResult(resultData));
                }
            }
            catch
            {
                return null;
            }

            return search;
        }
    }
}
