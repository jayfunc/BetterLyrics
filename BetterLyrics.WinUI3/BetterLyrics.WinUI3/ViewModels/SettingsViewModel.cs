using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Database;
using BetterLyrics.WinUI3.Services.Settings;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.ApplicationModel.Core;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SettingsViewModel(
        DatabaseService databaseService,
        SettingsService settingsService,
        MainViewModel mainViewModel
    ) : ObservableObject
    {
        private readonly MediaPlayer _mediaPlayer = new();

        private readonly DatabaseService _databaseService = databaseService;

        public SettingsService SettingsService => settingsService;

        public MainViewModel MainViewModel => mainViewModel;

        public string Version => Helper.AppInfo.AppVersion;

        [RelayCommand]
        private async Task RebuildLyricsIndexDatabaseAsync()
        {
            SettingsService.IsRebuildingLyricsIndexDatabase = true;
            await _databaseService.RebuildMusicMetadataIndexDatabaseAsync(
                SettingsService.MusicLibraries
            );
            SettingsService.IsRebuildingLyricsIndexDatabase = false;
        }

        public async Task RemoveFolderAsync(string path)
        {
            SettingsService.MusicLibraries.Remove(path);
            await RebuildLyricsIndexDatabaseAsync();
        }

        [RelayCommand]
        private async Task SelectAndAddFolderAsync()
        {
            var picker = new FolderPicker();

            picker.FileTypeFilter.Add("*");

            var hwnd = WindowNative.GetWindowHandle(App.Current.MainWindow);
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();

            App.Current.SettingsWindow!.AppWindow.MoveInZOrderAtTop();

            if (folder != null)
            {
                await AddFolderAsync(folder.Path);
            }
        }

        private async Task AddFolderAsync(string path)
        {
            bool existed = SettingsService.MusicLibraries.Any((x) => x == path);
            if (existed)
            {
                BaseWindow.StackedNotificationsBehavior?.Show(
                    App.ResourceLoader!.GetString("SettingsPagePathExistedInfo"),
                    Helper.AnimationHelper.StackedNotificationsShowingDuration
                );
            }
            else
            {
                SettingsService.MusicLibraries.Add(path);
                await RebuildLyricsIndexDatabaseAsync();
            }
        }

        [RelayCommand]
        private static async Task LaunchProjectGitHubPageAsync()
        {
            await Launcher.LaunchUriAsync(new Uri(Helper.AppInfo.GithubUrl));
        }

        private static void OpenFolderInFileExplorer(string path)
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = path,
                    UseShellExecute = true,
                }
            );
        }

        public static void OpenMusicFolder(string path)
        {
            OpenFolderInFileExplorer(path);
        }

        [RelayCommand]
        private static void RestartApp()
        {
            // The restart will be executed immediately.
            AppRestartFailureReason failureReason =
                Microsoft.Windows.AppLifecycle.AppInstance.Restart("");

            // If the restart fails, handle it here.
            switch (failureReason)
            {
                case AppRestartFailureReason.RestartPending:
                    break;
                case AppRestartFailureReason.NotInForeground:
                    break;
                case AppRestartFailureReason.InvalidUser:
                    break;
                default: //AppRestartFailureReason.Other
                    break;
            }
        }

        [RelayCommand]
        private async Task PlayTestingMusicTask()
        {
            await AddFolderAsync(Helper.AppInfo.AssetsFolder);
            _mediaPlayer.SetUriSource(new Uri(Helper.AppInfo.TestMusicPath));
            _mediaPlayer.Play();
        }

        [RelayCommand]
        private static void OpenLogFolder()
        {
            OpenFolderInFileExplorer(Helper.AppInfo.LogDirectory);
        }
    }
}
