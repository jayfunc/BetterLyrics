using BetterLyrics.WinUI3.Constants;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BetterLyrics.WinUI3.Helper
{
    public static class PlayerIDHelper
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

        public static bool IsBetterLyrics(string? id) => id is PlayerID.BetterLyrics or PlayerID.BetterLyricsDebug;

        public static string? GetDisplayName(string? id) => id switch
        {
            PlayerID.Spotify => PlayerName.Spotify,
            PlayerID.AppleMusic => PlayerName.AppleMusic,
            PlayerID.iTunes => PlayerName.iTunes,
            PlayerID.KugouMusic => PlayerName.KugouMusic,
            PlayerID.NetEaseCloudMusic => PlayerName.NetEaseCloudMusic,
            PlayerID.QQMusic => PlayerName.QQMusic,
            PlayerID.LXMusic => PlayerName.LXMusic,
            PlayerID.LXMusicPortable => PlayerName.LXMusicPortable,
            PlayerID.MediaPlayerWindows11 => PlayerName.MediaPlayerWindows11,
            PlayerID.AIMP => PlayerName.AIMP,
            PlayerID.Foobar2000 => PlayerName.Foobar2000,
            PlayerID.MusicBee => PlayerName.MusicBee,
            PlayerID.PotPlayer => PlayerName.PotPlayer,
            PlayerID.Chrome => PlayerName.Chrome,
            PlayerID.Edge => PlayerName.Edge,
            PlayerID.BetterLyrics => PlayerName.BetterLyrics,
            PlayerID.BetterLyricsDebug => PlayerName.BetterLyricsDebug,
            PlayerID.SaltPlayerForWindowsMS => PlayerName.SaltPlayerForWindowsMS,
            PlayerID.SaltPlayerForWindowsSteam => PlayerName.SaltPlayerForWindowsSteam,
            PlayerID.MoeKoeMusic => PlayerName.MoeKoeMusic,
            PlayerID.MoeKoeMusicAlternative => PlayerName.MoeKoeMusic,
            PlayerID.Listen1 => PlayerName.Listen1,
            _ => id,
        };

        public static string GetLogoPath(string? id) => id switch
        {
            PlayerID.Spotify => PathHelper.SpotifyLogoPath,
            PlayerID.AppleMusic => PathHelper.AppleMusicLogoPath,
            PlayerID.AppleMusicAlternative => PathHelper.AppleMusicLogoPath,
            PlayerID.iTunes => PathHelper.iTunesLogoPath,
            PlayerID.KugouMusic => PathHelper.KugouMusicLogoPath,
            PlayerID.NetEaseCloudMusic => PathHelper.NetEaseCloudMusicLogoPath,
            PlayerID.QQMusic => PathHelper.QQMusicLogoPath,
            PlayerID.LXMusic => PathHelper.LXMusicLogoPath,
            PlayerID.LXMusicPortable => PathHelper.LXMusicLogoPath,
            PlayerID.MediaPlayerWindows11 => PathHelper.MediaPlayerWindows11LogoPath,
            PlayerID.AIMP => PathHelper.AIMPLogoPath,
            PlayerID.Foobar2000 => PathHelper.Foobar2000LogoPath,
            PlayerID.MusicBee => PathHelper.MusicBeeLogoPath,
            PlayerID.PotPlayer => PathHelper.PotPlayerLogoPath,
            PlayerID.Chrome => PathHelper.ChromeLogoPath,
            PlayerID.Edge => PathHelper.EdgeLogoPath,
            PlayerID.BetterLyrics => PathHelper.LogoPath,
            PlayerID.BetterLyricsDebug => PathHelper.LogoPath,
            PlayerID.SaltPlayerForWindowsMS => PathHelper.SaltPlayerForWindowsLogoPath,
            PlayerID.SaltPlayerForWindowsSteam => PathHelper.SaltPlayerForWindowsLogoPath,
            PlayerID.MoeKoeMusic => PathHelper.MoeKoeMusicLogoPath,
            PlayerID.MoeKoeMusicAlternative => PathHelper.MoeKoeMusicLogoPath,
            PlayerID.Listen1 => PathHelper.Listen1LogoPath,
            _ => PathHelper.UnknownPlayerLogoPath,
        };
    }
}
