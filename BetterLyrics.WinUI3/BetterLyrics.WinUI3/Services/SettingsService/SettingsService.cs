// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using System;
using System.IO;
using System.Linq;

namespace BetterLyrics.WinUI3.Services.SettingsService
{
    // TODO 初始化时从文件读取到对象，后续独写操作先操纵对象，写入用 Debounce 写入文件
    // 新建一个 AppSettings 类
    public partial class SettingsService : ObservableObject, ISettingsService
    {
        private readonly DispatcherQueue _dispatcherQueue;
        private readonly DispatcherQueueTimer _dispatcherQueueTimer;

        [ObservableProperty]
        public partial AppSettings AppSettings { get; set; }

        public SettingsService()
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _dispatcherQueueTimer = _dispatcherQueue.CreateTimer();

            AppSettings = ReadAppSettings();
            AppSettings.PropertyChanged += AppSettings_PropertyChanged;

            // 确保当 LyricsSearchProvider 和 AlbumArtSearchProvider 枚举更新时，AppSettings 中的相关信息也能更新
            AppSettings.MediaSourceProvidersInfo = AppSettings.MediaSourceProvidersInfo.Select(x => new MediaSourceProviderInfo()
            {
                IsEnabled = x.IsEnabled,
                Provider = x.Provider,
                IsLastFMTrackEnabled = x.IsLastFMTrackEnabled,
                TimelineSyncThreshold = x.TimelineSyncThreshold,
                ResetPositionOffsetOnSongChanged = x.ResetPositionOffsetOnSongChanged,
                PositionOffset = x.PositionOffset,
                LyricsSearchProvidersInfo = [..Enum.GetValues<LyricsSearchProvider>().Select(p => new LyricsSearchProviderInfo(
                    p,
                    x.LyricsSearchProvidersInfo.Where(x => x.Provider == p).FirstOrDefault()?.IsEnabled ?? true
                ))],
                AlbumArtSearchProvidersInfo = [..Enum.GetValues<AlbumArtSearchProvider>().Select(p => new AlbumArtSearchProviderInfo(
                    p,
                    x.AlbumArtSearchProvidersInfo.Where(x => x.Provider == p).FirstOrDefault()?.IsEnabled ?? true
                ))],
            }).ToList();
        }

        private void AppSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
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
                SaveAppSettings();
            }, Constants.Time.DebounceTimeout);
        }

        private void SaveAppSettings()
        {
            File.WriteAllText(PathHelper.SettingsFilePath, System.Text.Json.JsonSerializer.Serialize(AppSettings, SourceGenerationContext.Default.AppSettings));
        }

    }
}
