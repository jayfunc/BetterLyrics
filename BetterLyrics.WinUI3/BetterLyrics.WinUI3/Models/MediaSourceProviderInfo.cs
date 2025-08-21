// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BetterLyrics.WinUI3.Models
{
    public partial class MediaSourceProviderInfo : ObservableRecipient
    {
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsEnabled { get; set; } = true;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial string Provider { get; set; }

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsLastFMTrackEnabled { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool IsTimelineSyncEnabled { get; set; } = true;
        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int TimelineSyncThreshold { get; set; }

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial int PositionOffset { get; set; }

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial bool ResetPositionOffsetOnSongChanged { get; set; } = false;

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial FullyObservableCollection<LyricsSearchProviderInfo> LyricsSearchProvidersInfo { get; set; } = [.. Enum.GetValues<LyricsSearchProvider>().Select(p => new LyricsSearchProviderInfo(p, true))];

        [ObservableProperty][NotifyPropertyChangedRecipients] public partial FullyObservableCollection<AlbumArtSearchProviderInfo> AlbumArtSearchProvidersInfo { get; set; } = [.. Enum.GetValues<AlbumArtSearchProvider>().Select(p => new AlbumArtSearchProviderInfo(p, true))];

        public MediaSourceProviderInfo()
        {
            Provider = string.Empty;
            TimelineSyncThreshold = 0;
            PositionOffset = 0;
        }

        public MediaSourceProviderInfo(string provider) : base()
        {
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
