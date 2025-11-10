using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BetterLyrics.WinUI3.Helper
{
    public static class PlayerIdMatcher
    {
        private static readonly List<string> neteaseFamilyRegex =
        [
            "cloudmusic.exe", //NetEaseCloudMusic
            "^17588BrandonWong\\.LyricEase_", //LyricEase
            "^48848aaaaaaccd\\.HyPlayer_" //HyPlayer
        ];

        public static bool IsNeteaseFamily(string id)
        {
            foreach (var regex in neteaseFamilyRegex)
            {
                var isMatch = Regex.IsMatch(id, regex);
                if (isMatch) return true;
            }
            return false;
        }

        public static bool IsLXMusic(string? id)
        {
            return id == Constants.PlayerID.LXMusic || id == Constants.PlayerID.LXMusicPortable;
        }
    }
}
