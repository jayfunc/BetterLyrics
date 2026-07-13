using System.Text.RegularExpressions;

namespace BetterLyrics.Core.Helpers;

public static class PlayerIdHelper
{
    private static readonly List<string> _neteaseFamilyRegex =
    [
        "cloudmusic.exe", //NetEaseCloudMusic
        "^17588BrandonWong\\.LyricEase_", //LyricEase
        "^48848aaaaaaccd\\.HyPlayer_" //HyPlayer
    ];

    private static readonly List<string> _qqFamilyRegex =
    [
        "QQMusic.exe"
    ];

    private static readonly List<string> _appleMusicRegex =
    [
        "AppleMusic.exe",
        "^AppleInc\\.AppleMusicWin_"
    ];

    private static readonly List<string> _betterLyricsRegex =
    [
        "^37412\\.BetterLyrics_"
    ];

    private static readonly List<string> _lxMusicRegex =
    [
        "cn.toside.music.desktop",
        "lx-music-desktop.exe"
    ];

    private static bool Is(string? id, List<string> regexes)
    {
        if (id is null) return false;

        foreach (var regex in regexes)
        {
            var isMatch = Regex.IsMatch(id, regex);
            if (isMatch) return true;
        }

        return false;
    }

    public static bool IsNeteaseFamily(string? id)
    {
        return Is(id, _neteaseFamilyRegex);
    }

    public static bool IsQQFamily(string? id)
    {
        return Is(id, _qqFamilyRegex);
    }

    public static bool IsLXMusic(string? id)
    {
        return Is(id, _lxMusicRegex);
    }

    public static bool IsAppleMusic(string? id)
    {
        return Is(id, _appleMusicRegex);
    }

    public static bool IsBetterLyrics(string? id)
    {
        return Is(id, _betterLyricsRegex);
    }
}