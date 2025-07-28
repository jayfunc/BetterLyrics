// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Globalization;
using Windows.UI;
using WinRT.Interop;
using MetadataHelper = BetterLyrics.WinUI3.Helper.MetadataHelper;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SettingsPageViewModel : BaseViewModel
    {
        private readonly ILibWatcherService _libWatcherService;
        private readonly IPlaybackService _playbackService;
        private readonly ITranslateService _libreTranslateService;

        private readonly string _autoStartupTaskId = "AutoStartup";

        private void PlaybackService_SessionIdsChanged(object? sender, Events.MediaSourceProvidersInfoEventArgs e)
        {
            MediaSourceProvidersInfo = [.. e.MediaSourceProviersInfo];
        }

        public void OnLyricsSearchProvidersReordered()
        {
            _settingsService.LyricsSearchProvidersInfo = [.. LyricsSearchProvidersInfo];
            Broadcast(
                LyricsSearchProvidersInfo,
                LyricsSearchProvidersInfo,
                nameof(LyricsSearchProvidersInfo)
            );
        }

        public void OnAlbumArtSearchProvidersReordered()
        {
            _settingsService.AlbumArtSearchProvidersInfo = [.. AlbumArtSearchProvidersInfo];
            Broadcast(
                AlbumArtSearchProvidersInfo,
                AlbumArtSearchProvidersInfo,
                nameof(AlbumArtSearchProvidersInfo)
            );
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

        public void ToggleLyricsSearchProvider()
        {
            _settingsService.LyricsSearchProvidersInfo = [.. LyricsSearchProvidersInfo];
            Broadcast(
                LyricsSearchProvidersInfo,
                LyricsSearchProvidersInfo,
                nameof(LyricsSearchProvidersInfo)
            );
        }

        public void ToggleAlbumArtSearchProvider(AlbumArtSearchProviderInfo providerInfo)
        {
            _settingsService.AlbumArtSearchProvidersInfo = [.. AlbumArtSearchProvidersInfo];
            Broadcast(
                AlbumArtSearchProvidersInfo,
                AlbumArtSearchProvidersInfo,
                nameof(AlbumArtSearchProvidersInfo)
            );
        }

        public void ToggleMediaSourceProvider(MediaSourceProviderInfo providerInfo)
        {
            Broadcast(
                MediaSourceProvidersInfo,
                MediaSourceProvidersInfo,
                nameof(MediaSourceProvidersInfo)
            );
        }

        private void AddFolderAsync(string path)
        {
            var normalizedPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

            if (LocalMediaFolders.Any(x => Path.GetFullPath(x.Path).TrimEnd(Path.DirectorySeparatorChar).Equals(normalizedPath.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)))
            {
                App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("SettingsPagePathExistedInfo"));
            }
            else if (LocalMediaFolders.Any(item => normalizedPath.StartsWith(Path.GetFullPath(item.Path).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            {
                // 添加的文件夹是现有文件夹的子文件夹
                App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("SettingsPagePathBeIncludedInfo"));
            }
            else if (LocalMediaFolders.Any(item => Path.GetFullPath(item.Path).TrimEnd(Path.DirectorySeparatorChar).StartsWith(normalizedPath, StringComparison.OrdinalIgnoreCase))
            )
            {
                // 添加的文件夹是现有文件夹的父文件夹
                App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("SettingsPagePathIncludingOthersInfo"));
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
            await Windows.System.Launcher.LaunchUriAsync(new Uri(MetadataHelper.GithubUrl));
        }

        [RelayCommand]
        private static async Task OpenCacheFolderAsync()
        {
            await Windows.System.Launcher.LaunchFolderPathAsync(PathHelper.CacheFolder);
        }

        [RelayCommand]
        private static void RestartApp()
        {
            WindowHelper.RestartApp();
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
                        App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("SettingsPageServerTestSuccessInfo"), Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success);
                        IsLibreTranslateServerTesting = false;
                    });
                }
                catch (Exception)
                {
                    _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                    {
                        App.Current.SettingsWindowNotificationPanel?.Notify(App.ResourceLoader!.GetString("SettingsPageServerTestFailedInfo"), Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error);
                        IsLibreTranslateServerTesting = false;
                    });
                }
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
                    App.Current.SettingsWindowNotificationPanel?.Notify(
                        App.ResourceLoader!.GetString($"SettingsPageServerTest{(testResult ? "Success" : "Failed")}Info"),
                        testResult ? InfoBarSeverity.Success : InfoBarSeverity.Error);
                    IsLXMusicServerTesting = false;
                });
            });
        }

        public async Task<bool> ToggleAutoStartupAsync(bool target)
        {
            StartupTask startupTask = await StartupTask.GetAsync(_autoStartupTaskId);
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
            var startupTask = await StartupTask.GetAsync(_autoStartupTaskId);
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
