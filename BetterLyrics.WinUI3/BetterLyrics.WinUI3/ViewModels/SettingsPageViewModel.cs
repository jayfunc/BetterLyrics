// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Helper.BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.Services.LastFMService;
using BetterLyrics.WinUI3.Services.LibWatcherService;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Services.TranslateService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Services.Store;
using Windows.Storage;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SettingsPageViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly IMediaSessionsService _mediaSessionsService;

        public string Version { get; set; } = MetadataHelper.AppVersion;

        [ObservableProperty]
        public partial AppSettings AppSettings { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsDebugOverlayEnabled { get; set; } = false;

        [ObservableProperty]
        public partial object NavViewSelectedItemTag { get; set; } = "App";

        public SettingsPageViewModel(ISettingsService settingsService, IMediaSessionsService mediaSessionsService)
        {
            _settingsService = settingsService;
            _mediaSessionsService = mediaSessionsService;
            AppSettings = _settingsService.AppSettings;
        }

        [RelayCommand]
        private async Task LaunchProjectGitHubPageAsync()
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(Constants.Link.GitHubUrl));
        }

        [RelayCommand]
        private static async Task OpenCacheFolderAsync()
        {
            await Windows.System.Launcher.LaunchFolderPathAsync(PathHelper.CacheFolder);
        }

        [RelayCommand]
        private static async Task OpenSettingsFolderAsync()
        {
            await Windows.System.Launcher.LaunchFolderPathAsync(PathHelper.LocalFolder);
        }

        [RelayCommand]
        private static void RestartApp()
        {
            WindowHelper.RestartApp();
        }

        [RelayCommand]
        private async Task ImportSettingsAsync()
        {
            var window = WindowHelper.GetWindowByWindowType<SettingsWindow>();
            if (window == null) return;

            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.FileTypeFilter.Add(".json");

            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);

            var file = await picker.PickSingleFileAsync();

            if (file != null)
            {
                var succeed = _settingsService.ImportSettings(file.Path);
                if (succeed)
                {
                    WindowHelper.RestartApp();
                }
                else
                {
                    App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader?.GetString("ImportSettingsFailed") ?? "");
                }
            }
        }

        [RelayCommand]
        private async Task ExportSettingsAsync()
        {
            var window = WindowHelper.GetWindowByWindowType<SettingsWindow>();
            if (window == null) return;

            var picker = new Windows.Storage.Pickers.FolderPicker();
            picker.FileTypeFilter.Add("*");

            var hwnd = WindowNative.GetWindowHandle(window);
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                _settingsService.ExportSettings(folder.Path);
                App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader?.GetString("ExportSettingsSuccess") ?? "", InfoBarSeverity.Success);
            }
        }

        [RelayCommand]
        private void ClearCacheFiles()
        {
            DirectoryHelper.DeleteAllFiles(PathHelper.LogDirectory);

            DirectoryHelper.DeleteAllFiles(PathHelper.LyricsCacheDirectory);
            DirectoryHelper.DeleteAllFiles(PathHelper.AmllTtmlDbLyricsCacheDirectory);
            DirectoryHelper.DeleteAllFiles(PathHelper.KugouLyricsCacheDirectory);
            DirectoryHelper.DeleteAllFiles(PathHelper.LrcLibLyricsCacheDirectory);
            DirectoryHelper.DeleteAllFiles(PathHelper.NeteaseLyricsCacheDirectory);
            DirectoryHelper.DeleteAllFiles(PathHelper.QQLyricsCacheDirectory);

            DirectoryHelper.DeleteAllFiles(PathHelper.TranslationCacheDirectory);
            DirectoryHelper.DeleteAllFiles(PathHelper.KugouTranslationCacheDirectory);
            DirectoryHelper.DeleteAllFiles(PathHelper.NeteaseTranslationCacheDirectory);
            DirectoryHelper.DeleteAllFiles(PathHelper.QQTranslationCacheDirectory);

            DirectoryHelper.DeleteAllFiles(PathHelper.iTunesAlbumArtCacheDirectory);

            App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("ActionCompleted"), InfoBarSeverity.Success);
        }
    }
}
