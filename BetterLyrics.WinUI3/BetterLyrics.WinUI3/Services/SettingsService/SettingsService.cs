// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Serialization;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using System;
using System.IO;
using System.Linq;
using Windows.Globalization;

namespace BetterLyrics.WinUI3.Services.SettingsService
{
    // TODO 初始化时从文件读取到对象，后续独写操作先操纵对象，写入用 Debounce 写入文件
    // 新建一个 AppSettings 类
    public partial class SettingsService : BaseViewModel, ISettingsService
    {
        public AppSettings AppSettings { get; set; }

        public SettingsService()
        {
            AppSettings = ReadAppSettings();

            AppSettings.PropertyChanged += AppSettings_PropertyChanged;

            AppSettings.TranslationSettings.PropertyChanged += AppSettings_PropertyChanged;
            AppSettings.GeneralSettings.PropertyChanged += AppSettings_PropertyChanged;
            AppSettings.MusicGallerySettings.PropertyChanged += AppSettings_PropertyChanged;
            AppSettings.AdvancedSettings.PropertyChanged += AppSettings_PropertyChanged;

            AppSettings.MediaSourceProvidersInfo.CollectionChanged += AppSettings_CollectionChanged;
            AppSettings.MediaSourceProvidersInfo.ItemPropertyChanged += AppSettings_ItemPropertyChanged;

            AppSettings.LocalMediaFolders.CollectionChanged += AppSettings_CollectionChanged;
            AppSettings.LocalMediaFolders.ItemPropertyChanged += AppSettings_ItemPropertyChanged;

            AppSettings.MappedSongSearchQueries.CollectionChanged += AppSettings_CollectionChanged;
            AppSettings.MappedSongSearchQueries.ItemPropertyChanged += AppSettings_ItemPropertyChanged;

            AppSettings.WindowBoundsRecords.CollectionChanged += AppSettings_CollectionChanged;
            AppSettings.WindowBoundsRecords.ItemPropertyChanged += AppSettings_ItemPropertyChanged;

            AppSettings.Version = MetadataHelper.AppVersion;

            EnsureMediaSourceProvidersInfo();
        }

        private void EnsureMediaSourceProvidersInfo()
        {
            // 确保当 LyricsSearchProvider 和 AlbumArtSearchProvider 枚举更新时，AppSettings 中的相关信息也能更新
            foreach (var x in AppSettings.MediaSourceProvidersInfo)
            {
                // 更新 LyricsSearchProvidersInfo
                foreach (var p in Enum.GetValues<LyricsSearchProvider>())
                {
                    var item = x.LyricsSearchProvidersInfo.FirstOrDefault(i => i.Provider == p);
                    if (item == null)
                    {
                        x.LyricsSearchProvidersInfo.Add(new LyricsSearchProviderInfo(p, true));
                    }
                    // 可根据需要更新 item.IsEnabled
                }
                // 移除多余项
                for (int i = x.LyricsSearchProvidersInfo.Count - 1; i >= 0; i--)
                {
                    if (!Enum.IsDefined(typeof(LyricsSearchProvider), x.LyricsSearchProvidersInfo[i].Provider))
                        x.LyricsSearchProvidersInfo.RemoveAt(i);
                }

                // 更新 AlbumArtSearchProvidersInfo
                foreach (var p in Enum.GetValues<AlbumArtSearchProvider>())
                {
                    var item = x.AlbumArtSearchProvidersInfo.FirstOrDefault(i => i.Provider == p);
                    if (item == null)
                    {
                        x.AlbumArtSearchProvidersInfo.Add(new AlbumArtSearchProviderInfo(p, true));
                    }
                    // 可根据需要更新 item.IsEnabled
                }
                for (int i = x.AlbumArtSearchProvidersInfo.Count - 1; i >= 0; i--)
                {
                    if (!Enum.IsDefined(typeof(AlbumArtSearchProvider), x.AlbumArtSearchProvidersInfo[i].Provider))
                        x.AlbumArtSearchProvidersInfo.RemoveAt(i);
                }
            }
        }

        private void AppSettings_ItemPropertyChanged(object? sender, ItemPropertyChangedEventArgs e)
        {
            WriteAppSettingsDebounce();
        }

        private void AppSettings_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            WriteAppSettingsDebounce();
        }

        private void AppSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(GeneralSettings.LanguageCode):
                    ApplicationLanguages.PrimaryLanguageOverride = AppSettings.GeneralSettings.LanguageCode;
                    break;
                default:
                    break;
            }
            WriteAppSettingsDebounce();
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
            // TODO 导入有问题
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
            if (!File.Exists(PathHelper.SettingsFilePath))
                return new AppSettings();

            var json = File.ReadAllText(PathHelper.SettingsFilePath);
            var data = System.Text.Json.JsonSerializer.Deserialize(json, SourceGenerationContext.Default.AppSettings);

            if (data == null)
                return new AppSettings();

            return data;
        }

        private void WriteAppSettingsDebounce()
        {
            _dispatcherQueueTimer.Debounce(() =>
            {
                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    SaveAppSettings();
                });
            }, Constants.Time.DebounceTimeout);
        }

        private void SaveAppSettings()
        {
            File.WriteAllText(PathHelper.SettingsFilePath, System.Text.Json.JsonSerializer.Serialize(AppSettings, SourceGenerationContext.Default.AppSettings));
        }
    }
}
