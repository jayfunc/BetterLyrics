using System.Text.RegularExpressions;

namespace BetterLyrics.Plugins.Transliteration.Romaji.Helpers
{
    public static class LrcParser
    {
        public static readonly Regex LrcLineRegex =
            new Regex("(\\[(\\d+:)?\\d+:\\d+(\\.\\d+)?\\])+\\s*(?<text>.*?(?=\\[(\\d+:)?\\d+:\\d+(\\.\\d+)?\\]|$|\\r|\\n))",
                RegexOptions.Compiled);

        public static readonly Regex LrcTimeRegex =
            new Regex("\\[((?<hour>\\d+):)?(?<minute>\\d+):(?<second>\\d+)(\\.(?<millisecond>\\d+))?\\]", RegexOptions.Compiled);

        public static List<(TimeSpan Time, string Text)> Parse(string lrc)
        {
            var result = new List<(TimeSpan Time, string Text)>();

            var lineMatches = LrcLineRegex.Matches(lrc);
            foreach (Match lineMatch in lineMatches)
            {
                var text = lineMatch.Groups["text"].Value;
                var timeMatches = LrcTimeRegex.Matches(lineMatch.Value);
                foreach (Match timeMatch in timeMatches)
                {
                    var hour = timeMatch.Groups["hour"].Success ? int.Parse(timeMatch.Groups["hour"].Value) : 0;
                    var minute = timeMatch.Groups["minute"].Success ? int.Parse(timeMatch.Groups["minute"].Value) : 0;
                    var second = timeMatch.Groups["second"].Success ? int.Parse(timeMatch.Groups["second"].Value) : 0;
                    var millisecond = timeMatch.Groups["millisecond"].Success
                        ? int.Parse(timeMatch.Groups["millisecond"].Value)
                        : 0;
                    var time = new TimeSpan(0, hour, minute, second, millisecond);
                    result.Add((time, text));
                }
            }

            return result;
        }
    }
}