using BetterLyrics.WinUI3.Constants;
using BetterLyrics.WinUI3.Helper;
using Microsoft.UI.Xaml.Data;
using System;

namespace BetterLyrics.WinUI3.Converter
{
    public class MediaSourceProviderToLogoUriConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string provider)
            {
                return provider switch
                {
                    PlayerID.Spotify => PathHelper.SpotifyLogoPath,
                    PlayerID.AppleMusic => PathHelper.AppleMusicLogoPath,
                    PlayerID.AppleMusicAlternative => PathHelper.AppleMusicLogoPath,
                    PlayerID.iTunes  => PathHelper.iTunesLogoPath,
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
                    PlayerID.SaltPlayerForWindows => PathHelper.SaltPlayerForWindowsLogoPath,
                    PlayerID.MoeKoeMusic => PathHelper.MoeKoeMusicLogoPath,
                    PlayerID.MoeKoeMusicAlternative => PathHelper.MoeKoeMusicLogoPath,
                    PlayerID.Listen1 => PathHelper.Listen1LogoPath,
                    _ => PathHelper.UnknownPlayerLogoPath,
                };
            }
            return PathHelper.UnknownPlayerLogoPath;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
