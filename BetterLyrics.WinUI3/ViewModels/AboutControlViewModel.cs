using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Helper.BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.AppUpdateService;
using BetterLyrics.WinUI3.Services.LyricsCacheService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Windows.System;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class AboutControlViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly ILyricsCacheService _lyricsCacheService;

        [ObservableProperty] public partial IAppUpdateService AppUpdateService { get; set; }

        [ObservableProperty] public partial AppSettings AppSettings { get; set; }

        public AboutControlViewModel(ISettingsService settingsService, ILyricsCacheService lyricsCacheService, IAppUpdateService appUpdateService)
        {
            _settingsService = settingsService;
            _lyricsCacheService = lyricsCacheService;
            AppUpdateService = appUpdateService;

            AppSettings = _settingsService.AppSettings;
        }

        [RelayCommand]
        private static async Task LaunchProjectGitHubPageAsync()
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri(Constants.Link.BetterLyricsGitHub));
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
        private async Task ImportSettingsAsync()
        {
            var file = await PickerHelper.PickSingleFileAsync<SettingsWindow>([".zip"]);

            if (file != null)
            {
                try
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();

                    SqliteConnection.ClearAllPools();

                    string tempExtractPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                    Directory.CreateDirectory(tempExtractPath);

                    using (var stream = await file.OpenStreamForReadAsync())
                    {
                        ZipFile.ExtractToDirectory(stream, tempExtractPath);
                    }

                    DirectoryHelper.CopyDirectory(tempExtractPath, PathHelper.LocalFolder, true);

                    Directory.Delete(tempExtractPath, true);

                    WindowHook.RestartApp();
                }
                catch (Exception ex)
                {
                    GlobalToastManager.Show("ImportSettingsFailed", ex.Message, InfoBarSeverity.Error);
                }
            }
        }

        [RelayCommand]
        private async Task ExportSettingsAsync()
        {
            try
            {
                var suggestedFileName = $"{Constants.App.AppName}_{_settingsService.AppSettings.Version}_{DateTime.Now:yyyyMMdd_HHmmss}";
                IDictionary<string, IList<string>> fileTypeChoices = new Dictionary<string, IList<string>>()
                {
                    { "Zip Archive", new List<string>() { ".zip" } }
                };

                var destinationFile = await PickerHelper.PickSaveFileAsync<SettingsWindow>(fileTypeChoices, suggestedFileName);
                if (destinationFile == null) return;

                string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempDir);

                DirectoryHelper.CopyDirectory(PathHelper.LocalFolder, tempDir, true);

                string tempZipPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");

                ZipFile.CreateFromDirectory(tempDir, tempZipPath);

                using (var sourceStream = File.OpenRead(tempZipPath))
                using (var destStream = await destinationFile.OpenStreamForWriteAsync())
                {
                    sourceStream.CopyTo(destStream);
                    destStream.SetLength(sourceStream.Length);
                }

                Directory.Delete(tempDir, true);
                File.Delete(tempZipPath);

                GlobalToastManager.Show("ExportSettingsSuccess", null, InfoBarSeverity.Success);
            }
            catch (Exception ex)
            {
                GlobalToastManager.Show("Error", ex.Message, InfoBarSeverity.Error);
            }
        }

        [RelayCommand]
        private async Task ClearCacheFilesAsync()
        {
            await _lyricsCacheService.ClearCacheAsync();

            DirectoryHelper.DeleteAllFiles(PathHelper.LogDirectory);
            DirectoryHelper.DeleteAllFiles(PathHelper.LyricsCacheDirectory);
            DirectoryHelper.DeleteAllFiles(PathHelper.iTunesAlbumArtCacheDirectory);

            GlobalToastManager.Show("ActionCompleted", null, InfoBarSeverity.Success);
        }

        [RelayCommand]
        private static async Task OpenAppStorePageAsync()
        {
            await Launcher.LaunchUriAsync(new Uri(Constants.Link.StorePage));
        }

        [RelayCommand]
        private async Task CheckAppUpdateAsync()
        {
            await AppUpdateService.UpdateAvailabilityAsync();
        }

    }
}
