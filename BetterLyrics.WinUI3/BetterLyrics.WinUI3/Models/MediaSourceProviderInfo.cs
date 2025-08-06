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

        public MediaSourceProviderInfo() { }

        public MediaSourceProviderInfo(string provider)
        {
            Provider = provider;
            IsEnabled = true;
            IsLastFMTrackEnabled = false;
            if (provider == Constants.PlayerID.AppleMusic)
            {
                TimelineSyncThreshold = PositionOffset = 1000;
            }
            else
            {
                TimelineSyncThreshold = 0;
                PositionOffset = 0;
            }
            ResetPositionOffsetOnSongChanged = false;
            LyricsSearchProvidersInfo = [.. Enum.GetValues<LyricsSearchProvider>().Select(p => new LyricsSearchProviderInfo(p, true))];
        }

    }
}
