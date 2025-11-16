// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Collections;
using BetterLyrics.WinUI3.Helper;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Linq;
using BetterLyrics.WinUI3.Constants;

namespace BetterLyrics.WinUI3.Models
{
    public partial class MediaSourceProviderInfo : ObservableRecipient
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsEnabled { get; set; } = true;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string Provider { get; set; }

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLastFMTrackEnabled { get; set; } = false;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsDiscordPresenceEnabled { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsTimelineSyncEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int TimelineSyncThreshold { get; set; }

        /// <summary>
        /// Unit: ms
        /// </summary>
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int PositionOffset { get; set; }

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool ResetPositionOffsetOnSongChanged { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial FullyObservableCollection<LyricsSearchProviderInfo> LyricsSearchProvidersInfo { get; set; } = [.. Enum.GetValues<LyricsSearchProvider>().Select(p => new LyricsSearchProviderInfo(p, true))];

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial FullyObservableCollection<AlbumArtSearchProviderInfo> AlbumArtSearchProvidersInfo { get; set; } = [.. Enum.GetValues<AlbumArtSearchProvider>().Select(p => new AlbumArtSearchProviderInfo(p, true))];

        public bool IsLXMusic => PlayerIDMatcher.IsLXMusic(Provider);

        public string DisplayName => Provider switch
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
            _ => Provider,
        };

        public string LogoPath => Provider switch
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
            PlayerID.SaltPlayerForWindows => PathHelper.SaltPlayerForWindowsLogoPath,
            PlayerID.MoeKoeMusic => PathHelper.MoeKoeMusicLogoPath,
            PlayerID.MoeKoeMusicAlternative => PathHelper.MoeKoeMusicLogoPath,
            PlayerID.Listen1 => PathHelper.Listen1LogoPath,
            _ => PathHelper.UnknownPlayerLogoPath,
        };

        public MediaSourceProviderInfo()
        {
            Provider = string.Empty;
            TimelineSyncThreshold = 0;
            PositionOffset = 0;
        }

        public MediaSourceProviderInfo(string provider, bool isEnable = true)
        {
            IsEnabled = isEnable;
            switch (provider)
            {
                case Constants.PlayerID.AppleMusic:
                    // Apple Music 的特性
                    TimelineSyncThreshold = 1000;
                    PositionOffset = 1000;
                    break;
                default:
                    // 设置 300 以防不必要的重复同步
                    TimelineSyncThreshold = 300;
                    PositionOffset = 0;
                    break;
            }

            Provider = provider;

            AlbumArtSearchProvidersInfo.ItemPropertyChanged += AlbumArtSearchProvidersInfo_ItemPropertyChanged;
            AlbumArtSearchProvidersInfo.CollectionChanged += AlbumArtSearchProvidersInfo_CollectionChanged;

            LyricsSearchProvidersInfo.ItemPropertyChanged += LyricsSearchProvidersInfo_ItemPropertyChanged;
            LyricsSearchProvidersInfo.CollectionChanged += LyricsSearchProvidersInfo_CollectionChanged;
        }

        partial void OnAlbumArtSearchProvidersInfoChanged(FullyObservableCollection<AlbumArtSearchProviderInfo> oldValue, FullyObservableCollection<AlbumArtSearchProviderInfo> newValue)
        {
            oldValue?.CollectionChanged -= AlbumArtSearchProvidersInfo_CollectionChanged;
            oldValue?.ItemPropertyChanged -= AlbumArtSearchProvidersInfo_ItemPropertyChanged;
            newValue?.CollectionChanged += AlbumArtSearchProvidersInfo_CollectionChanged;
            newValue?.ItemPropertyChanged += AlbumArtSearchProvidersInfo_ItemPropertyChanged;
        }

        partial void OnLyricsSearchProvidersInfoChanged(FullyObservableCollection<LyricsSearchProviderInfo> oldValue, FullyObservableCollection<LyricsSearchProviderInfo> newValue)
        {
            oldValue?.CollectionChanged -= LyricsSearchProvidersInfo_CollectionChanged;
            oldValue?.ItemPropertyChanged -= LyricsSearchProvidersInfo_ItemPropertyChanged;
            newValue?.CollectionChanged += LyricsSearchProvidersInfo_CollectionChanged;
            newValue?.ItemPropertyChanged += LyricsSearchProvidersInfo_ItemPropertyChanged;
        }

        private void AlbumArtSearchProvidersInfo_ItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(AlbumArtSearchProvidersInfo));
        }

        private void AlbumArtSearchProvidersInfo_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(AlbumArtSearchProvidersInfo));
        }

        private void LyricsSearchProvidersInfo_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged(nameof(LyricsSearchProvidersInfo));
        }

        private void LyricsSearchProvidersInfo_ItemPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnPropertyChanged(nameof(LyricsSearchProvidersInfo));
        }

    }
}
