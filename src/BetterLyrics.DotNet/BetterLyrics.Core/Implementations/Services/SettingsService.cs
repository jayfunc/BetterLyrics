// 2025/6/23 by Zhe Fang

using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json;
using BetterLyrics.Core.Collections;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.Serialization;
using BetterLyrics.Core.ViewModels;
using Microsoft.Extensions.Logging;

namespace BetterLyrics.Core.Implementations.Services;

public class SettingsService : BaseViewModel, ISettingsService
{
    private readonly IAppUIThreadProvider _appUiThreadProvider;
    private readonly ILocalizationService _localizationService;
    private readonly ISystemUIProvider _systemUiProvider;
    private readonly ILogger<SettingsService> _logger;
    private readonly Debouncer _writeAppSettingsDebouncer = new();

    public SettingsService(ILocalizationService localizationService, IAppUIThreadProvider appUiThreadProvider,
        ISystemUIProvider systemUiProvider, ILogger<SettingsService> logger)
    {
        _localizationService = localizationService;
        _appUiThreadProvider = appUiThreadProvider;
        _systemUiProvider = systemUiProvider;
        _logger = logger;

        AppSettings = ReadAppSettings();

        AppSettings.PropertyChanged += AppSettings_PropertyChanged;

        AppSettings.TranslationSettings.PropertyChanged += AppSettings_PropertyChanged;
        AppSettings.GeneralSettings.PropertyChanged += AppSettings_PropertyChanged;
        AppSettings.MusicGallerySettings.PropertyChanged += AppSettings_PropertyChanged;
        AppSettings.AdvancedSettings.PropertyChanged += AppSettings_PropertyChanged;
        AppSettings.LyricsSaveConfig.PropertyChanged += AppSettings_PropertyChanged;
        AppSettings.SystemTraySettings.PropertyChanged += AppSettings_PropertyChanged;
        AppSettings.LyricsCardSettings.PropertyChanged += AppSettings_PropertyChanged;
        AppSettings.DiscordSettings.PropertyChanged += AppSettings_PropertyChanged;

        AppSettings.MediaSourceProvidersInfo.CollectionChanged += AppSettings_CollectionChanged;
        AppSettings.MediaSourceProvidersInfo.ItemPropertyChanged += AppSettings_ItemPropertyChanged;

        AppSettings.LocalMediaFolders.CollectionChanged += AppSettings_CollectionChanged;
        AppSettings.LocalMediaFolders.ItemPropertyChanged += AppSettings_ItemPropertyChanged;

        AppSettings.MappedSongSearchQueries.CollectionChanged += AppSettings_CollectionChanged;
        AppSettings.MappedSongSearchQueries.ItemPropertyChanged += AppSettings_ItemPropertyChanged;

        AppSettings.WindowBoundsRecords.CollectionChanged += AppSettings_CollectionChanged;
        AppSettings.WindowBoundsRecords.ItemPropertyChanged += AppSettings_ItemPropertyChanged;

        AppSettings.StarredPlaylists.CollectionChanged += AppSettings_CollectionChanged;
        AppSettings.StarredPlaylists.ItemPropertyChanged += AppSettings_ItemPropertyChanged;

        AppSettings.PluginsInfo.CollectionChanged += AppSettings_CollectionChanged;
        AppSettings.PluginsInfo.ItemPropertyChanged += AppSettings_ItemPropertyChanged;

        AppSettings.LyricsCardConfigs.CollectionChanged += AppSettings_CollectionChanged;
        AppSettings.LyricsCardConfigs.ItemPropertyChanged += AppSettings_ItemPropertyChanged;

        AppSettings.LayoutProfiles.CollectionChanged += AppSettings_CollectionChanged;
        AppSettings.LayoutProfiles.ItemPropertyChanged += AppSettings_ItemPropertyChanged;

        AppSettings.MusicGallerySettings.PlayQueuePaths.CollectionChanged += AppSettings_CollectionChanged;

        AppSettings.Version = MetadataHelper.AppVersion;

        _logger.LogInformation("App version: {AppVersion}", AppSettings.Version);

        EnsureMediaSourceProvidersInfo();
        EnsureStarredPlaylists();
        EnsureLayoutProfiles();
        EnsureLyricsWindowStatus();
    }

    public AppSettings AppSettings { get; set; }

    /// <summary>
    ///     Export settings to specific folder
    /// </summary>
    /// <param name="exportPath">Target folder path (not file path)</param>
    public void ExportSettings(string exportPath)
    {
        // 导出到文件
        var exportJson =
            JsonSerializer.Serialize(AppSettings, SourceGenerationContext.Default.AppSettings);
        File.WriteAllText(
            Path.Combine(exportPath, $"BetterLyrics_Settings_Export_{DateTime.Now:yyyyMMdd_HHmmss}.json"),
            exportJson);
    }

    /// <summary>
    ///     Indicate a value whether import action is successfullt done
    /// </summary>
    /// <param name="importPath"></param>
    /// <returns></returns>
    public bool ImportSettings(string importPath)
    {
        if (!File.Exists(importPath))
            return false;

        var importJson = File.ReadAllText(importPath);
        var importData =
            JsonSerializer.Deserialize(importJson, SourceGenerationContext.Default.AppSettings);

        if (importData == null)
            return false;

        AppSettings = importData;
        SaveAppSettings();
        return true;
    }

    private void EnsureMediaSourceProvidersInfo()
    {
        foreach (var x in AppSettings.MediaSourceProvidersInfo)
        {
            // 同步歌词提供源
            SyncProviderInfo(
                x.LyricsSearchProvidersInfo,
                p => p.Provider,
                p => new LyricsSearchProviderInfo(p, true),
                p => p.IsInternal()
            );

            // 同步封面提供源
            SyncProviderInfo(
                x.AlbumArtSearchProvidersInfo,
                p => p.Provider,
                p => new AlbumArtSearchProviderInfo(p, true)
            );
        }
    }

    private void EnsureLyricsWindowStatus()
    {
        var records = AppSettings.WindowBoundsRecords;
        var layoutProfiles = AppSettings.LayoutProfiles;
        if (records.Count == 0)
            foreach (var mode in Enum.GetValues<LyricsWindowMode>())
                records.Add(new LyricsWindowStatus(mode)
                {
                    IsDefault = mode == LyricsWindowMode.Standard
                });

        foreach (var item in records)
            if (item.LayoutProfileId == Guid.Empty)
            {
                var mode = item.GetDefaultLayoutProfileMode();
                var layoutProfile = layoutProfiles.FirstOrDefault(p => p.Mode == mode);
                if (layoutProfile != null) item.LayoutProfileId = layoutProfile.Id;
            }

        var playerLyricsWindowStatus = AppSettings.MusicGallerySettings.LyricsWindowStatus;
        if (playerLyricsWindowStatus.LayoutProfileId == Guid.Empty)
        {
            var layoutProfile =
                layoutProfiles.FirstOrDefault(p => p.Mode == NowPlayingLayoutMode.LeftAlbumArtRightLyrics);
            if (layoutProfile != null) playerLyricsWindowStatus.LayoutProfileId = layoutProfile.Id;
        }
    }

    private void EnsureLayoutProfiles()
    {
        foreach (var mode in Enum.GetValues<NowPlayingLayoutMode>())
        {
            if (mode == NowPlayingLayoutMode.Custom) continue;

            var existing = AppSettings.LayoutProfiles.FirstOrDefault(p => p.Mode == mode);
            if (existing == null)
            {
                AppSettings.LayoutProfiles.Add(new LayoutProfile(mode));
            }
            else
            {
                var id = existing.Id;
                var index = AppSettings.LayoutProfiles.IndexOf(existing);
                AppSettings.LayoutProfiles[index] = new LayoutProfile(mode)
                {
                    Id = id
                };
            }
        }
    }

    /// <summary>
    ///     通用同步方法：仅管理枚举值 < 1000 的项
    /// </summary>
    private void SyncProviderInfo<TEnum, TItem>(
        IList<TItem> collection,
        Func<TItem, TEnum> enumSelector,
        Func<TEnum, TItem> itemFactory,
        Func<TEnum, bool>? shouldExclude = null)
        where TEnum : struct, Enum
        where TItem : INotifyPropertyChanged
    {
        var allEnums = Enum.GetValues<TEnum>();
        var targetValidEnums = new HashSet<TEnum>();

        foreach (var e in allEnums)
        {
            if (Convert.ToInt32(e) >= 1000) continue;
            if (shouldExclude != null && shouldExclude(e)) continue;
            targetValidEnums.Add(e);
        }

        var itemsToRemove = collection.Where(item =>
        {
            var enumVal = enumSelector(item);
            var intVal = Convert.ToInt32(enumVal);

            if (intVal >= 1000) return false;

            return !targetValidEnums.Contains(enumVal);
        }).ToList();

        foreach (var item in itemsToRemove) collection.Remove(item);

        var existingEnums = collection.Select(enumSelector).ToHashSet();

        foreach (var p in targetValidEnums)
            if (!existingEnums.Contains(p))
                collection.Add(itemFactory(p));
    }

    private void EnsureStarredPlaylists()
    {
        if (!AppSettings.StarredPlaylists.Any(x => x.IsDefault))
            AppSettings.StarredPlaylists.Insert(0, new SongsTabInfo
            {
                Name = _localizationService.GetLocalizedString("MusicGalleryPageAllSongs"),
                Icon = "\uE8A9",
                FilterProperty = CommonSongProperty.Title,
                FilterValue = string.Empty
            });
    }

    private void AppSettings_ItemPropertyChanged(object? sender, ItemPropertyChangedEventArgs e)
    {
        WriteAppSettings();
    }

    private void AppSettings_CollectionChanged(object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        WriteAppSettings();
    }

    private void AppSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GeneralSettings.LanguageCode))
            _systemUiProvider.SetAppLanguage(AppSettings.GeneralSettings.LanguageCode);
        WriteAppSettings();
    }

    private static AppSettings ReadAppSettings()
    {
        return SettingsIO.ReadSettings(PathHelper.SettingsFilePath,
            SourceGenerationContext.Default.AppSettings);
    }

    private void WriteAppSettings()
    {
        _ = _writeAppSettingsDebouncer.RunAsync(() => { _appUiThreadProvider.Execute(SaveAppSettings); });
    }

    private void SaveAppSettings()
    {
        SettingsIO.SaveSettings(PathHelper.SettingsFilePath, AppSettings,
            SourceGenerationContext.Default.AppSettings);
    }
}