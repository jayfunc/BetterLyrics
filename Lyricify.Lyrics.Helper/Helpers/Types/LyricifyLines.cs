using System.Text.RegularExpressions;

namespace Lyricify.Lyrics.Helpers.Types
{
    public static class LyricifyLines
    {
        public static bool IsLyricifyLines(string input)
        {
            if (input == null) return false;

            if (input.Contains("[type:LyricifyLines]")) return true;

            var regex = new Regex(@"^\[\d+,\d+\].*");
            var matches = regex.Matches(input);
            if (matches.Count > 0)
            {
                var syllableRegex = new Regex(@"\w+\(\d+,\d+\)");
                var syllableMatches = syllableRegex.Matches(input);
                return syllableMatches.Count <= matches.Count;
            }

            return false;
        }
    }
}
