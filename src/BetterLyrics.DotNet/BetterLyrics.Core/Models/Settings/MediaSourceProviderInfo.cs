// 2025/6/23 by Zhe Fang

using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json.Serialization;
using BetterLyrics.Core.Collections;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Models.Memory;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.Models.Settings;

public partial class MediaSourceProviderInfo : ObservableRecipient
{
    public MediaSourceProviderInfo()
    {
        Provider = string.Empty;
        TimelineSyncThreshold = 0;
        PositionOffset = 0;
    }

    public MediaSourceProviderInfo(string provider, bool isEnable = true)
    {
        IsEnabled = isEnable;
        if (PlayerIdHelper.IsAppleMusic(provider))
        {
            // Apple Music 的特性
            TimelineSyncThreshold = 1000;
            PositionOffset = 1000;
        }
        else
        {
            // 设置 300 以防不必要的重复同步
            TimelineSyncThreshold = 300;
            PositionOffset = 0;
        }

        Provider = provider;

        AlbumArtSearchProvidersInfo.ItemPropertyChanged += AlbumArtSearchProvidersInfo_ItemPropertyChanged;
        AlbumArtSearchProvidersInfo.CollectionChanged += AlbumArtSearchProvidersInfo_CollectionChanged;

        LyricsSearchProvidersInfo.ItemPropertyChanged += LyricsSearchProvidersInfo_ItemPropertyChanged;
        LyricsSearchProvidersInfo.CollectionChanged += LyricsSearchProvidersInfo_CollectionChanged;
    }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsMemoryReaderEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsMemoryReaderRiskAccepted { get; set; } = false;

    [ObservableProperty] public partial MemoryReaderConfig? MemoryReaderConfig { get; set; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial string Provider { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsLastFMTrackEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsDiscordPresenceEnabled { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool IsTimelineSyncEnabled { get; set; } = true;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int TimelineSyncThreshold { get; set; }

    /// <summary>
    ///     Unit: ms
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int PositionOffset { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial bool ResetPositionOffsetOnSongChanged { get; set; } = false;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial LyricsSearchType LyricsSearchType { get; set; } = LyricsSearchType.Sequential;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int MatchingThreshold { get; set; } = 60;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial FullyObservableCollection<LyricsSearchProviderInfo> LyricsSearchProvidersInfo { get; set; } =
        [.. Enum.GetValues<LyricsProvider>().Where(p => !p.IsInternal()).Select(p => new LyricsSearchProviderInfo(p, true))];

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial int TargetAlbumArtSize { get; set; } = 500;

    [ObservableProperty]
    [NotifyPropertyChangedRecipients]
    public partial FullyObservableCollection<AlbumArtSearchProviderInfo> AlbumArtSearchProvidersInfo { get; set; } =
        [.. Enum.GetValues<AlbumArtSearchProvider>().Select(p => new AlbumArtSearchProviderInfo(p, true))];

    [JsonIgnore] public bool IsLXMusic => PlayerIdHelper.IsLXMusic(Provider);
    [JsonIgnore] public bool IsBetterLyrics => PlayerIdHelper.IsBetterLyrics(Provider);
    [JsonIgnore] [ObservableProperty] public partial bool IsFocused { get; set; } = false;

    partial void OnAlbumArtSearchProvidersInfoChanged(FullyObservableCollection<AlbumArtSearchProviderInfo> oldValue,
        FullyObservableCollection<AlbumArtSearchProviderInfo> newValue)
    {
        oldValue?.CollectionChanged -= AlbumArtSearchProvidersInfo_CollectionChanged;
        oldValue?.ItemPropertyChanged -= AlbumArtSearchProvidersInfo_ItemPropertyChanged;
        newValue?.CollectionChanged += AlbumArtSearchProvidersInfo_CollectionChanged;
        newValue?.ItemPropertyChanged += AlbumArtSearchProvidersInfo_ItemPropertyChanged;
    }

    partial void OnLyricsSearchProvidersInfoChanged(FullyObservableCollection<LyricsSearchProviderInfo> oldValue,
        FullyObservableCollection<LyricsSearchProviderInfo> newValue)
    {
        oldValue?.CollectionChanged -= LyricsSearchProvidersInfo_CollectionChanged;
        oldValue?.ItemPropertyChanged -= LyricsSearchProvidersInfo_ItemPropertyChanged;
        newValue?.CollectionChanged += LyricsSearchProvidersInfo_CollectionChanged;
        newValue?.ItemPropertyChanged += LyricsSearchProvidersInfo_ItemPropertyChanged;
    }

    private void AlbumArtSearchProvidersInfo_ItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(AlbumArtSearchProvidersInfo));
    }

    private void AlbumArtSearchProvidersInfo_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(AlbumArtSearchProvidersInfo));
    }

    private void LyricsSearchProvidersInfo_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(LyricsSearchProvidersInfo));
    }

    private void LyricsSearchProvidersInfo_ItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(LyricsSearchProvidersInfo));
    }
}