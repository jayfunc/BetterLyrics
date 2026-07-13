namespace BetterLyrics.Core.Helpers;

public static class StringHelper
{
    public static string NewLine = "\n";

    // 去除空格、括号、下划线、横杠、点、大小写等
    public static string Normalize(string s)
    {
        return new string(s
                .Where(c => char.IsLetterOrDigit(c))
                .ToArray())
            .ToLowerInvariant();
    }

    public static bool IsSwitchableNormalizedMatch(string source, string q1, string q2)
    {
        var normFileName = Normalize(source);
        var normQ1 = Normalize(q1);
        var normQ2 = Normalize(q2);

        // 常见两种顺序
        return normFileName == normQ1 + normQ2
               || normFileName == normQ2 + normQ1;
    }
}