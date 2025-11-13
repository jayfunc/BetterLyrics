using BetterLyrics.WinUI3.Constants;
using Microsoft.UI.Xaml.Data;
using System;

namespace BetterLyrics.WinUI3.Converter
{
    public class MediaSourceProviderToDisplayedNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string provider)
            {
                return provider switch
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
                    PlayerID.SaltPlayerForWindows => PlayerName.SaltPlayerForWindows,
                    PlayerID.MoeKoeMusic => PlayerName.MoeKoeMusic,
                    PlayerID.MoeKoeMusicAlternative => PlayerName.MoeKoeMusic,
                    PlayerID.Listen1 => PlayerName.Listen1,
                    _ => provider,
                };
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
