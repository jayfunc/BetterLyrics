// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BetterLyrics.WinUI3.Models
{
    public partial class MediaSourceProviderInfo : ObservableObject
    {
        [ObservableProperty]
        public partial bool IsEnabled { get; set; }

        [ObservableProperty]
        public partial string Provider { get; set; }

        [ObservableProperty]
        public partial bool IsLastFMTrackEnabled { get; set; }

        [ObservableProperty]
        public partial int TimelineSyncThreshold { get; set; }

        [ObservableProperty]
        public partial int PositionOffset { get; set; }

        [ObservableProperty]
        public partial bool ResetPositionOffsetOnSongChanged { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<LyricsSearchProviderInfo> LyricsSearchProvidersInfo { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<AlbumArtSearchProviderInfo> AlbumArtSearchProvidersInfo { get; set; }

        public MediaSourceProviderInfo() { }

        public MediaSourceProviderInfo(string provider)
        {
            switch (provider)
            {
                case Constants.PlayerID.AppleMusic:
                    TimelineSyncThreshold = 1000;
                    PositionOffset = 1000;
                    break;
                default:
                    TimelineSyncThreshold = 0;
                    PositionOffset = 0;
                    break;
            }

            Provider = provider;
            IsEnabled = true;
            IsLastFMTrackEnabled = false;
            ResetPositionOffsetOnSongChanged = false;
            LyricsSearchProvidersInfo = [.. Enum.GetValues<LyricsSearchProvider>().Select(p => new LyricsSearchProviderInfo(p, true))];
            AlbumArtSearchProvidersInfo = [.. Enum.GetValues<AlbumArtSearchProvider>().Select(p => new AlbumArtSearchProviderInfo(p, true))];
        }

    }
}
