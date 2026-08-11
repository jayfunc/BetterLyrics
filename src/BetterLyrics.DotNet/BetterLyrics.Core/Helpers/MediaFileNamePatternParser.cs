using System.Text.RegularExpressions;

namespace BetterLyrics.Core.Helpers;

public static class MediaFileNamePatternParser
{
    public static (string? Artist, string? Title, string? Album) Parse(string fileNameWithoutExt, string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return (null, null, null);

        try
        {
            var escapedPattern = ToRegexPattern(pattern);
            
            var regex = new Regex(escapedPattern, RegexOptions.IgnoreCase);
            var match = regex.Match(fileNameWithoutExt);

            if (match.Success)
            {
                var artist = match.Groups["Artist"].Success ? match.Groups["Artist"].Value.Trim() : null;
                var title = match.Groups["Title"].Success ? match.Groups["Title"].Value.Trim() : null;
                var album = match.Groups["Album"].Success ? match.Groups["Album"].Value.Trim() : null;
                
                return (artist, title, album);
            }
        }
        catch
        {
            // Ignore regex parsing errors
        }

        return (null, null, null);
    }

    public static string ToRegexPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern)) return string.Empty;

        var escapedPattern = Regex.Escape(pattern);
        escapedPattern = escapedPattern.Replace("\\{Artist\\}", "(?<Artist>.*?)");
        escapedPattern = escapedPattern.Replace("\\{Title\\}", "(?<Title>.*?)");
        escapedPattern = escapedPattern.Replace("\\{Album\\}", "(?<Album>.*?)");

        // Treat any other {xyz} as an ignored wildcard
        escapedPattern = Regex.Replace(escapedPattern, @"\\\{.*?\\\}", ".*?");

        return "^" + escapedPattern + "$";
    }
}
