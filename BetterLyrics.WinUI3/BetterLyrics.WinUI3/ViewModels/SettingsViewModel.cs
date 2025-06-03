using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Database;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Storage.Pickers;
using Windows.System;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.ViewModels {
    public partial class SettingsViewModel : ObservableObject {

        private readonly DatabaseService _databaseService;

        [ObservableProperty]
        private SettingsService _settingsService;

        [ObservableProperty]
        private string _version;

        public SettingsViewModel(DatabaseService databaseService, SettingsService settingsService) {
            _databaseService = databaseService;
            _settingsService = settingsService;

            var version = Package.Current.Id.Version;
            Version = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        [RelayCommand]
        private async Task RebuildLyricsIndexDatabaseAsync() {
            SettingsService.IsRebuildingLyricsIndexDatabase = true;
            await _databaseService.RebuildMusicMetadataIndexDatabaseAsync(SettingsService.MusicLibraries);
            SettingsService.IsRebuildingLyricsIndexDatabase = false;
        }

        [RelayCommand]
        private async Task RemoveFolderAsync(MusicFolder musicFolder) {
            SettingsService.MusicLibraries.Remove(musicFolder);
            await RebuildLyricsIndexDatabaseAsync();
        }

        [RelayCommand]
        private async Task AddFolderAsync() {
            var picker = new FolderPicker();

            picker.FileTypeFilter.Add("*");

            var hwnd = WindowNative.GetWindowHandle(App.Current.MainWindow);
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();

            if (folder != null) {
                string path = folder.Path;
                bool existed = SettingsService.MusicLibraries.Count((x) => x.Path == path) > 0;
                if (existed) {
                    MainWindow.StackedNotificationsBehavior?.Show(App.ResourceLoader.GetString("SettingsPagePathExistedInfo"), 3900);
                } else {
                    SettingsService.MusicLibraries.Add(new MusicFolder(path));
                    await RebuildLyricsIndexDatabaseAsync();
                }
            }

        }

        [RelayCommand]
        private async Task LaunchProjectGitHubPageAsync() {
            await Launcher.LaunchUriAsync(new Uri("https://github.com/jayfunc/BetterLyrics"));
        }

        [RelayCommand]
        private void OpenFolderInFileExplorer(MusicFolder musicFolder) {
            Process.Start(new ProcessStartInfo {
                FileName = "explorer.exe",
                Arguments = musicFolder.Path,
                UseShellExecute = true
            });
        }

        [RelayCommand]
        private void RestartApp() {
            // The restart will be executed immediately.
            AppRestartFailureReason failureReason =
                Microsoft.Windows.AppLifecycle.AppInstance.Restart("");

            // If the restart fails, handle it here.
            switch (failureReason) {
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

    }
}
