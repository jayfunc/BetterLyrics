namespace BetterLyrics.Plugins.Romaji.Extensions
{
    public static class StringExtension
    {
        public static string[] LineToUnits(this string str)
        {
            return str.Split(new[] { ' ', '　' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}