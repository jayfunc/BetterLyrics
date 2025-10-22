using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BetterLyrics.WinUI3.Helper
{
    public static class PlayerIdMatcher
    {
        private static readonly List<string> _neteaseFamilyRegex =
        [
            "cloudmusic.exe", //NetEaseCloudMusic
            "^17588BrandonWong\\.LyricEase_", //LyricEase
            "^48848aaaaaaccd\\.HyPlayer_" //HyPlayer
        ];

        public static bool IsNeteaseFamily(string player)
        {
            foreach (var regex in _neteaseFamilyRegex)
            {
                var isMatch = Regex.IsMatch(player, regex);
                if (isMatch) return true;
            }
            return false;
        }
    }
}
