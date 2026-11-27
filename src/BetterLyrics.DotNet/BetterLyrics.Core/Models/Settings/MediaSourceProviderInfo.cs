// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Collections;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Helpers.MediaFieldCorrection;
using BetterLyrics.Core.Messages.MediaLibrary;
using BetterLyrics.Core.Models.Memory;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace BetterLyrics.Core.Models.Settings;

public partial class MediaSourceProviderInfo : ObservableObject
{
    public MediaSourceProviderInfo()
    {
        Provider = string.Empty;
        TimelineSyncThreshold = 0;
        PositionOffset = 0;

        // 旧的配置文件里没有 FieldMappingRules 字段，反序列化不会触发属性赋值，
        // 这里先挂上通知，保证之后添加的规则能够正常保存并触发歌曲信息刷新。
        AttachFieldMappingRulesHandlers(FieldMappingRules);
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

        AttachFieldMappingRulesHandlers(FieldMappingRules);

        // 播放器一旦被记录，立即写入内置默认规则（目前只有 Apple Music 需要）
        EnsureFieldCorrectionRulesSeeded();
    }

    [ObservableProperty]
    public partial bool IsEnabled { get; set; } = true;

    [ObservableProperty]
    public partial bool IsMemoryReaderEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsMemoryReaderRiskAccepted { get; set; } = false;

    [ObservableProperty] public partial MemoryReaderConfig? MemoryReaderConfig { get; set; } = new();

    [ObservableProperty]
    public partial string Provider { get; set; }

    [ObservableProperty]
    public partial bool IsLastFMTrackEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsListenBrainzTrackEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsDiscordPresenceEnabled { get; set; } = false;

    [ObservableProperty]
    public partial bool IsTimelineSyncEnabled { get; set; } = true;

    [ObservableProperty]
    public partial int TimelineSyncThreshold { get; set; }

    /// <summary>
    /// Unit: ms
    /// </summary>
    [ObservableProperty]
    public partial int PositionOffset { get; set; }

    [ObservableProperty]
    public partial bool ResetPositionOffsetOnSongChanged { get; set; } = false;

    [ObservableProperty]
    public partial LyricsSearchType LyricsSearchType { get; set; } = LyricsSearchType.Sequential;

    [ObservableProperty]
    public partial int MatchingThreshold { get; set; } = 60;

    [ObservableProperty]
    public partial FullyObservableCollection<LyricsSearchProviderInfo> LyricsSearchProvidersInfo { get; set; } =
        [.. Enum.GetValues<LyricsProvider>().Where(p => !p.IsInternal()).Select(p => new LyricsSearchProviderInfo(p, true))];

    [ObservableProperty]
    public partial int TargetAlbumArtSize { get; set; } = 500;

    [ObservableProperty]
    public partial FullyObservableCollection<AlbumArtSearchProviderInfo> AlbumArtSearchProvidersInfo { get; set; } =
        [.. Enum.GetValues<AlbumArtProvider>().Select(p => new AlbumArtSearchProviderInfo(p, true))];

    /// <summary>
    /// 该播放器的字段纠正规则，按列表顺序依次执行。
    /// </summary>
    [ObservableProperty]
    public partial FullyObservableCollection<MediaFieldMappingRule> FieldMappingRules { get; set; } = [];

    /// <summary>
    /// 是否已经写入过内置默认规则。
    /// 用户删除默认规则后，下次启动不会再自动加回来。
    /// </summary>
    [ObservableProperty]
    public partial bool IsFieldCorrectionSeeded { get; set; } = false;

    [JsonIgnore] public bool IsLXMusic => PlayerIdHelper.IsLXMusic(Provider);
    [JsonIgnore] public bool IsBetterLyrics => PlayerIdHelper.IsBetterLyrics(Provider);
    [JsonIgnore][ObservableProperty] public partial bool IsFocused { get; set; } = false;

    partial void OnIsEnabledChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new MediaSourceProviderEnabledChangedMessage(this, value));

    partial void OnIsDiscordPresenceEnabledChanged(bool value) =>
        WeakReferenceMessenger.Default.Send(new MediaSourceDiscordPresenceChangedMessage(this, value));

    partial void OnTargetAlbumArtSizeChanged(int value) =>
        WeakReferenceMessenger.Default.Send(new MediaSourceTargetAlbumArtSizeChangedMessage(this, value));

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

    partial void OnFieldMappingRulesChanged(FullyObservableCollection<MediaFieldMappingRule> oldValue,
        FullyObservableCollection<MediaFieldMappingRule> newValue)
    {
        oldValue?.CollectionChanged -= FieldMappingRules_CollectionChanged;
        oldValue?.ItemPropertyChanged -= FieldMappingRules_ItemPropertyChanged;
        AttachFieldMappingRulesHandlers(newValue);
    }

    private void AttachFieldMappingRulesHandlers(FullyObservableCollection<MediaFieldMappingRule>? rules)
    {
        if (rules == null) return;

        rules.CollectionChanged += FieldMappingRules_CollectionChanged;
        rules.ItemPropertyChanged += FieldMappingRules_ItemPropertyChanged;
    }

    private void FieldMappingRules_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // 冒泡给上一层（设置持久化与歌曲信息刷新都依赖这个通知）
        OnPropertyChanged(nameof(FieldMappingRules));
    }

    private void FieldMappingRules_ItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(nameof(FieldMappingRules));
    }

    /// <summary>
    /// 首次见到该播放器时写入内置默认规则。
    /// 只在没有写入过的情况下执行，避免用户清空规则后又被自动加回来。
    /// </summary>
    public void EnsureFieldCorrectionRulesSeeded()
    {
        // 配置文件里显式写成 null 时也能正常恢复
        if (FieldMappingRules is null) FieldMappingRules = [];

        // 更早版本的规则只有 Text 参数、没有 Pattern，已经无法执行，直接清掉并允许重新种入默认规则
        var removedLegacyRules = false;
        for (var i = FieldMappingRules.Count - 1; i >= 0; i--)
        {
            if (!string.IsNullOrWhiteSpace(FieldMappingRules[i].Pattern)) continue;

            FieldMappingRules.RemoveAt(i);
            removedLegacyRules = true;
        }

        if (removedLegacyRules) IsFieldCorrectionSeeded = false;

        if (IsFieldCorrectionSeeded) return;

        IsFieldCorrectionSeeded = true;

        if (FieldMappingRules.Count > 0) return;
        if (!PlayerIdHelper.IsAppleMusic(Provider)) return;

        foreach (var rule in MediaFieldCorrectionPresets.CreateAppleMusicRules())
        {
            FieldMappingRules.Add(rule);
        }
    }
}