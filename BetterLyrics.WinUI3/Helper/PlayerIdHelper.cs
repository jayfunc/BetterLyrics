using BetterLyrics.WinUI3.Constants;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BetterLyrics.WinUI3.Helper
{
    public static class PlayerIdHelper
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

        public static bool IsLXMusic(string? id) => id is PlayerId.LXMusic or PlayerId.LXMusicPortable;

        public static bool IsAppleMusic(string? id) => id is PlayerId.AppleMusic or PlayerId.AppleMusicAlternative;

        public static bool IsBetterLyrics(string? id) => id is PlayerId.BetterLyrics or PlayerId.BetterLyricsDebug;

        public static string? GetDisplayName(string? id) => id switch
        {
            PlayerId.Spotify => PlayerName.Spotify,
            PlayerId.AppleMusic => PlayerName.AppleMusic,
            PlayerId.iTunes => PlayerName.iTunes,
            PlayerId.KugouMusic => PlayerName.KugouMusic,
            PlayerId.NetEaseCloudMusic => PlayerName.NetEaseCloudMusic,
            PlayerId.QQMusic => PlayerName.QQMusic,
            PlayerId.LXMusic => PlayerName.LXMusic,
            PlayerId.LXMusicPortable => PlayerName.LXMusicPortable,
            PlayerId.MediaPlayerWindows11 => PlayerName.MediaPlayerWindows11,
            PlayerId.AIMP => PlayerName.AIMP,
            PlayerId.Foobar2000 => PlayerName.Foobar2000,
            PlayerId.MusicBee => PlayerName.MusicBee,
            PlayerId.PotPlayer => PlayerName.PotPlayer,
            PlayerId.Chrome => PlayerName.Chrome,
            PlayerId.Edge => PlayerName.Edge,
            PlayerId.BetterLyrics => PlayerName.BetterLyrics,
            PlayerId.BetterLyricsDebug => PlayerName.BetterLyricsDebug,
            PlayerId.SaltPlayerForWindowsMS => PlayerName.SaltPlayerForWindowsMS,
            PlayerId.SaltPlayerForWindowsSteam => PlayerName.SaltPlayerForWindowsSteam,
            PlayerId.MoeKoeMusic => PlayerName.MoeKoeMusic,
            PlayerId.MoeKoeMusicAlternative => PlayerName.MoeKoeMusic,
            PlayerId.Listen1 => PlayerName.Listen1,
            PlayerId.OriginalSoundHQPlayer => PlayerName.OriginalSoundHQPlayer,
            PlayerId.JRiverMediaCenter => PlayerName.JRiverMediaCenter,
            _ => id,
        };

        public static string GetLogoPath(string? id) => id switch
        {
            PlayerId.Spotify => PathHelper.SpotifyLogoPath,
            PlayerId.AppleMusic => PathHelper.AppleMusicLogoPath,
            PlayerId.AppleMusicAlternative => PathHelper.AppleMusicLogoPath,
            PlayerId.iTunes => PathHelper.iTunesLogoPath,
            PlayerId.KugouMusic => PathHelper.KugouMusicLogoPath,
            PlayerId.NetEaseCloudMusic => PathHelper.NetEaseCloudMusicLogoPath,
            PlayerId.QQMusic => PathHelper.QQMusicLogoPath,
            PlayerId.LXMusic => PathHelper.LXMusicLogoPath,
            PlayerId.LXMusicPortable => PathHelper.LXMusicLogoPath,
            PlayerId.MediaPlayerWindows11 => PathHelper.MediaPlayerWindows11LogoPath,
            PlayerId.AIMP => PathHelper.AIMPLogoPath,
            PlayerId.Foobar2000 => PathHelper.Foobar2000LogoPath,
            PlayerId.MusicBee => PathHelper.MusicBeeLogoPath,
            PlayerId.PotPlayer => PathHelper.PotPlayerLogoPath,
            PlayerId.Chrome => PathHelper.ChromeLogoPath,
            PlayerId.Edge => PathHelper.EdgeLogoPath,
            PlayerId.BetterLyrics => PathHelper.LogoPath,
            PlayerId.BetterLyricsDebug => PathHelper.LogoPath,
            PlayerId.SaltPlayerForWindowsMS => PathHelper.SaltPlayerForWindowsLogoPath,
            PlayerId.SaltPlayerForWindowsSteam => PathHelper.SaltPlayerForWindowsLogoPath,
            PlayerId.MoeKoeMusic => PathHelper.MoeKoeMusicLogoPath,
            PlayerId.MoeKoeMusicAlternative => PathHelper.MoeKoeMusicLogoPath,
            PlayerId.Listen1 => PathHelper.Listen1LogoPath,
            PlayerId.OriginalSoundHQPlayer => PathHelper.OriginalSoundHQPlayerLogoPath,
            _ => PathHelper.UnknownPlayerLogoPath,
        };
    }
}
