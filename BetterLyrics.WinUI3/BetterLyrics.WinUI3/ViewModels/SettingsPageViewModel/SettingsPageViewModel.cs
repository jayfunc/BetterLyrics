// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.Services.LastFMService;
using BetterLyrics.WinUI3.Services.LibWatcherService;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.TranslateService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.ViewModels.SettingsPageViewModel
{
    public partial class SettingsPageViewModel : BaseViewModel
    {
        private readonly ILibWatcherService _libWatcherService;
        private readonly IMediaSessionsService _mediaSessionsService;
        private readonly ITranslateService _libreTranslateService;
        private readonly ILastFMService _lastFMService;

        private void MediaSessionsService_SessionIdsChanged(object? sender, Events.MediaSourceProvidersInfoEventArgs e)
        {
            MediaSourceProvidersInfo = [.. e.MediaSourceProviersInfo];
        }

        public void RemoveFolderAsync(LocalMediaFolder folder)
        {
            LocalMediaFolders.Remove(folder);
            _settingsService.LocalMediaFolders = [.. LocalMediaFolders];
            _libWatcherService.UpdateWatchers([.. LocalMediaFolders]);
            Broadcast(LocalMediaFolders, LocalMediaFolders, nameof(LocalMediaFolders));
        }

        public void ToggleLocalLyricsFolder()
        {
            _settingsService.LocalMediaFolders = [.. LocalMediaFolders];
            Broadcast(LocalMediaFolders, LocalMediaFolders, nameof(LocalMediaFolders));
        }

        public void BroadcastMediaSourceProvidersInfoChanged()
        {
            _dispatcherQueueTimer.Debounce(() =>
            {
                _settingsService.MediaSourceProvidersInfo = [.. MediaSourceProvidersInfo];
                Broadcast(
                    MediaSourceProvidersInfo,
                    MediaSourceProvidersInfo,
                    nameof(MediaSourceProvidersInfo)
                );
            }, TimeSpan.FromMilliseconds(100));
        }

        private void AddFolderAsync(string path)
        {
            var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (LocalMediaFolders.Any(x => Path.GetFullPath(x.Path).TrimEnd(Path.DirectorySeparatorChar).Equals(normalizedPath.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)))
            {
                App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("SettingsPagePathExistedInfo"), InfoBarSeverity.Warning);
            }
            else if (LocalMediaFolders.Any(item => normalizedPath.StartsWith(Path.GetFullPath(item.Path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            {
                // 添加的文件夹是现有文件夹的子文件夹
                App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("SettingsPagePathBeIncludedInfo"), InfoBarSeverity.Warning);
            }
            else if (LocalMediaFolders.Any(item => Path.GetFullPath(item.Path).TrimEnd(Path.DirectorySeparatorChar).StartsWith(normalizedPath, StringComparison.OrdinalIgnoreCase))
            )
            {
                // 添加的文件夹是现有文件夹的父文件夹
                App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("SettingsPagePathIncludingOthersInfo"), InfoBarSeverity.Warning);
            }
            else
            {
                LocalMediaFolders.Add(new LocalMediaFolder(path, true));
                _settingsService.LocalMediaFolders = [.. LocalMediaFolders];
                _libWatcherService.UpdateWatchers([.. LocalMediaFolders]);
                Broadcast(LocalMediaFolders, LocalMediaFolders, nameof(LocalMediaFolders));
            }
        }

        [RelayCommand]
        private async Task LaunchProjectGitHubPageAsync()
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(Constants.Link.GithubUrl));
        }

        [RelayCommand]
        private static async Task OpenCacheFolderAsync()
        {
            await Windows.System.Launcher.LaunchFolderPathAsync(Helper.PathHelper.CacheFolder);
        }

        [RelayCommand]
        private static void RestartApp()
        {
            Helper.WindowHelper.RestartApp();
        }

        [RelayCommand]
        private async Task SelectAndAddFolderAsync(UIElement sender)
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
                AddFolderAsync(folder.Path);
            }
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
            }
        }

        [RelayCommand]
        private void LibreTranslateServerTest()
        {
            IsLibreTranslateServerTesting = true;
            Task.Run(async () =>
            {
                try
                {
                    string targetLangCode = LanguageHelper.SupportedTargetLanguages[SelectedTargetLanguageIndex].Code;
                    string result = await _libreTranslateService.TranslateTextAsync("Hello, world!", targetLangCode, null);
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                    {
                        App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("SettingsPageServerTestSuccessInfo"), InfoBarSeverity.Success);
                    });
                }
                catch (Exception)
                {
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                    {
                        App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("SettingsPageServerTestFailedInfo"), InfoBarSeverity.Error);
                    });
                }
                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    IsLibreTranslateServerTesting = false;
                });
            });
        }

        [RelayCommand]
        private void LXMusicServerTest()
        {
            IsLXMusicServerTesting = true;
            Task.Run(async () =>
            {
                bool testResult = await NetHelper.CheckConnectivity($"{LXMusicServer}/status");
                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                {
                    if (testResult)
                    {
                        App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("SettingsPageServerTestSuccessInfo"), InfoBarSeverity.Success);
                    }
                    else
                    {
                        App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("SettingsPageServerTestFailedInfo"), InfoBarSeverity.Error);
                    }
                    IsLXMusicServerTesting = false;
                });
            });
        }

        [RelayCommand]
        private void RefreshMonitorDeviceNames()
        {
            MonitorDeviceNames = [.. MonitorHelper.GetAllMonitorDeviceNames()];
            SelectedDockMonitorDeviceName = MonitorHelper.GetPrimaryMonitorDeviceName();
        }

        [RelayCommand]
        private async Task LastFMAuthAsync()
        {
            await _lastFMService.AuthAsync();
        }

        [RelayCommand]
        private async Task LastFMUnAuthAsync()
        {
            await _lastFMService.UnAuthAsync();
        }

        [RelayCommand]
        private async Task LastFMRefreshAsync()
        {
            await _lastFMService.RefreshAsync();
        }

        public async Task<bool> ToggleAutoStartupAsync(bool target)
        {
            StartupTask startupTask = await StartupTask.GetAsync(Constants.App.AutoStartupTaskId);
            if (target)
            {
                await startupTask.RequestEnableAsync();
            }
            else
            {
                startupTask.Disable();
            }
            return await DetectIsAutoStartupEnabledAsync();
        }

        public async Task<bool> DetectIsAutoStartupEnabledAsync()
        {
            bool result = false;
            var startupTask = await StartupTask.GetAsync(Constants.App.AutoStartupTaskId);
            switch (startupTask.State)
            {
                case StartupTaskState.Disabled:
                case StartupTaskState.DisabledByUser:
                case StartupTaskState.DisabledByPolicy:
                    result = false;
                    break;
                case StartupTaskState.Enabled:
                    result = true;
                    break;
            }
            return result;
        }
    }
}
