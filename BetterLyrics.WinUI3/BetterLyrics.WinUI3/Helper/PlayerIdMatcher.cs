using BetterLyrics.WinUI3.Constants;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BetterLyrics.WinUI3.Helper
{
    public static class PlayerIDMatcher
    {
        private static readonly List<string> neteaseFamilyRegex =
        [
            "cloudmusic.exe", //NetEaseCloudMusic
            "^17588BrandonWong\\.LyricEase_", //LyricEase
            "^48848aaaaaaccd\\.HyPlayer_" //HyPlayer
        ];

        public static bool IsNeteaseFamily(string? id)
        {
            if (id is null) return false;

            foreach (var regex in neteaseFamilyRegex)
            {
                var isMatch = Regex.IsMatch(id, regex);
                if (isMatch) return true;
            }
            return false;
        }

        public static bool IsLXMusic(string? id) => id is PlayerID.LXMusic or PlayerID.LXMusicPortable;

        public static bool IsAppleMusic(string? id) => id is PlayerID.AppleMusic or PlayerID.AppleMusicAlternative;
    }
}
