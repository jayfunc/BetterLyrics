namespace BetterLyrics.Core.Constants;

public static class App
{
    public const string AppAuthor = "Zhe Fang";
    public const string AppAuthorNicknameEN = "jayfunc";
    public const string AppAuthorNicknameZH = "摘叶飞镖";

    public const string AutoStartupTaskId = "AutoStartup";
    public const string StoreId = "9p1wcd1p597r";

    public const string SloganCN = "曲拨心弦，词落云笺。";
    public const string SloganJP = "琴線に響くメロディ、雲箋に綴るフレーズ。";

    public const string SloganEN = "Strums the Heartstrings, Graces the Wordscapes.";

#if WINDOWS
     public static string AppName = Windows.ApplicationModel.Package.Current.Id.FamilyName == "37412.BetterLyrics_rd1g0rsrrtxw8" ? "BetterLyrics" : "BetterLyrics (Dev)";
#else
    public static string AppName = "BetterLyrics";
#endif
}