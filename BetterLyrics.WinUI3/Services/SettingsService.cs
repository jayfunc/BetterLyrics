// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Collections;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.Serialization;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.ViewModels;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Globalization;

namespace BetterLyrics.WinUI3.Services
{
    public partial class SettingsService : BaseViewModel, ISettingsService
    {
        private readonly Debouncer _writeAppSettingsDebouncer = new();
        private readonly ILocalizationService _localizationService;

        public AppSettings AppSettings { get; set; }

        public SettingsService(ILocalizationService localizationService)
        {
            _localizationService = localizationService;

            AppSettings = ReadAppSettings();

            AppSettings.PropertyChanged += AppSettings_PropertyChanged;

            AppSettings.TranslationSettings.PropertyChanged += AppSettings_PropertyChanged;
            AppSettings.GeneralSettings.PropertyChanged += AppSettings_PropertyChanged;
            AppSettings.MusicGallerySettings.PropertyChanged += AppSettings_PropertyChanged;
            AppSettings.AdvancedSettings.PropertyChanged += AppSettings_PropertyChanged;
            AppSettings.LyricsSaveConfig.PropertyChanged += AppSettings_PropertyChanged;
            AppSettings.SystemTraySettings.PropertyChanged += AppSettings_PropertyChanged;

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

            EnsureMediaSourceProvidersInfo();
            EnsureStarredPlaylists();
            EnsureLayoutProfiles();
            EnsureLyricsWindowStatus();
        }

        private void EnsureMediaSourceProvidersInfo()
        {
            foreach (var x in AppSettings.MediaSourceProvidersInfo)
            {
                // 同步歌词提供源
                SyncProviderInfo<LyricsSearchProvider, LyricsSearchProviderInfo>(
                    x.LyricsSearchProvidersInfo,
                    p => p.Provider,
                    p => new LyricsSearchProviderInfo(p, true)
                );

                // 同步封面提供源
                SyncProviderInfo<AlbumArtSearchProvider, AlbumArtSearchProviderInfo>(
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
            {
                foreach (var mode in Enum.GetValues<LyricsWindowMode>().Cast<LyricsWindowMode>())
                {
                    records.Add(new LyricsWindowStatus(mode)
                    {
                        IsDefault = mode == LyricsWindowMode.Standard,
                    });
                }
            }

            foreach (var item in records)
            {
                if (item.LayoutProfileId == Guid.Empty)
                {
                    var mode = item.GetDefaultLayoutProfileMode();
                    var layoutProfile = layoutProfiles.FirstOrDefault(p => p.Mode == mode);
                    if (layoutProfile != null)
                    {
                        item.LayoutProfileId = layoutProfile.Id;
                    }
                }
            }

            var playerLyricsWindowStatus = AppSettings.MusicGallerySettings.LyricsWindowStatus;
            if (playerLyricsWindowStatus.LayoutProfileId == Guid.Empty)
            {
                var layoutProfile = layoutProfiles.FirstOrDefault(p => p.Mode == NowPlayingLayoutMode.LeftAlbumArtRightLyrics);
                if (layoutProfile != null)
                {
                    playerLyricsWindowStatus.LayoutProfileId = layoutProfile.Id;
                }
            }
        }

        private void EnsureLayoutProfiles()
        {
            foreach (var mode in Enum.GetValues<NowPlayingLayoutMode>().Cast<NowPlayingLayoutMode>())
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
                        Id = id,
                    };
                }
            }
        }

        /// <summary>
        /// 通用同步方法：仅管理枚举值 < 1000 的项
        /// </summary>
        private void SyncProviderInfo<TEnum, TItem>(
            IList<TItem> collection,
            Func<TItem, TEnum> enumSelector,
            Func<TEnum, TItem> itemFactory)
            where TEnum : struct, Enum
            where TItem : System.ComponentModel.INotifyPropertyChanged
        {
            var allEnums = Enum.GetValues<TEnum>();
            var targetValidEnums = new HashSet<TEnum>();

            foreach (var e in allEnums)
            {
                if (Convert.ToInt32(e) < 1000)
                {
                    targetValidEnums.Add(e);
                }
            }

            var itemsToRemove = collection.Where(item =>
            {
                var enumVal = enumSelector(item);
                int intVal = Convert.ToInt32(enumVal);

                if (intVal >= 1000) return false;

                return !targetValidEnums.Contains(enumVal);
            }).ToList();

            foreach (var item in itemsToRemove)
            {
                collection.Remove(item);
            }

            var existingEnums = collection.Select(enumSelector).ToHashSet();

            foreach (var p in targetValidEnums)
            {
                if (!existingEnums.Contains(p))
                {
                    collection.Add(itemFactory(p));
                }
            }
        }

        private void EnsureStarredPlaylists()
        {
            if (!AppSettings.StarredPlaylists.Any(x => x.IsDefault))
            {
                AppSettings.StarredPlaylists.Insert(0, new SongsTabInfo
                {
                    Name = _localizationService.GetLocalizedString("MusicGalleryPageAllSongs"),
                    Icon = "\uE8A9",
                    FilterProperty = CommonSongProperty.Title,
                    FilterValue = string.Empty
                });
            }
        }

        private void AppSettings_ItemPropertyChanged(object? sender, ItemPropertyChangedEventArgs e)
        {
            WriteAppSettings();
        }

        private void AppSettings_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            WriteAppSettings();
        }

        private void AppSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GeneralSettings.LanguageCode))
            {
                ApplicationLanguages.PrimaryLanguageOverride = AppSettings.GeneralSettings.LanguageCode;
            }
            //else if (e.PropertyName == nameof(GeneralSettings.EnhanceControlInteractiveAnimations))
            //{
            //    UpdateGlobalStyles(AppSettings.GeneralSettings.EnhanceControlInteractiveAnimations);
            //}
            WriteAppSettings();
        }

        public void UpdateGlobalStyles(bool useCustom)
        {
            var mergedDicts = Application.Current.Resources.MergedDictionaries;

            var fluentDict = mergedDicts.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("FluentStyles.xaml"));
            var defaultDict = mergedDicts.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("DefaultStyles.xaml"));

            if (useCustom)
            {
                if (fluentDict == null)
                {
                    mergedDicts.Add(new ResourceDictionary { Source = new System.Uri("ms-appx:///Themes/FluentStyles.xaml") });
                }
                if (defaultDict != null)
                {
                    mergedDicts.Remove(defaultDict);
                }
            }
            else
            {
                if (defaultDict == null)
                {
                    mergedDicts.Add(new ResourceDictionary { Source = new System.Uri("ms-appx:///Themes/DefaultStyles.xaml") });
                }
                if (fluentDict != null)
                {
                    mergedDicts.Remove(fluentDict);
                }
            }
        }

        /// <summary>
        /// Export settings to specific folder
        /// </summary>
        /// <param name="exportPath">Target folder path (not file path)</param>
        public void ExportSettings(string exportPath)
        {
            // 导出到文件
            var exportJson = System.Text.Json.JsonSerializer.Serialize(AppSettings, SourceGenerationContext.Default.AppSettings);
            File.WriteAllText(Path.Combine(exportPath, $"BetterLyrics_Settings_Export_{DateTime.Now:yyyyMMdd_HHmmss}.json"), exportJson);
        }

        /// <summary>
        /// Indicate a value whether import action is successfullt done
        /// </summary>
        /// <param name="importPath"></param>
        /// <returns></returns>
        public bool ImportSettings(string importPath)
        {
            if (!File.Exists(importPath))
                return false;

            var importJson = File.ReadAllText(importPath);
            var importData = System.Text.Json.JsonSerializer.Deserialize(importJson, SourceGenerationContext.Default.AppSettings);

            if (importData == null)
                return false;

            AppSettings = importData;
            SaveAppSettings();
            return true;
        }

        private static AppSettings ReadAppSettings()
        {
            return Core.Helpers.SettingsIO.ReadSettings(PathHelper.SettingsFilePath, SourceGenerationContext.Default.AppSettings);
        }

        private void WriteAppSettings()
        {
            _ = _writeAppSettingsDebouncer.RunAsync(() =>
            {
                AppUIThread.Execute(SaveAppSettings);
            });
        }

        private void SaveAppSettings()
        {
            Core.Helpers.SettingsIO.SaveSettings(PathHelper.SettingsFilePath, AppSettings, SourceGenerationContext.Default.AppSettings);
        }
    }
}
