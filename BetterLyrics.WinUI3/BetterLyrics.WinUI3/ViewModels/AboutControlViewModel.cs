using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Helper.BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.ResourceService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class AboutControlViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly IResourceService _resourceService;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsDebugOverlayEnabled { get; set; } = false;

        [ObservableProperty]
        public partial AppSettings AppSettings { get; set; }

        public AboutControlViewModel(ISettingsService settingsService, IResourceService resourceService)
        {
            _settingsService = settingsService;
            _resourceService = resourceService;

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
        private async Task ImportSettingsAsync()
        {
            var file = await PickerHelper.PickSingleFileAsync<SettingsWindow>([".json"]);

            if (file != null)
            {
                var succeed = _settingsService.ImportSettings(file.Path);
                if (succeed)
                {
                    WindowHook.RestartApp();
                }
                else
                {
                    DevWinUI.Growl.Error(_resourceService.GetLocalizedString("ImportSettingsFailed") ?? "");
                }
            }
        }

        [RelayCommand]
        private async Task ExportSettingsAsync()
        {
            var folder = await PickerHelper.PickSingleFolderAsync<SettingsWindow>();

            if (folder != null)
            {
                _settingsService.ExportSettings(folder.Path);
                DevWinUI.Growl.Success(_resourceService.GetLocalizedString("ExportSettingsSuccess") ?? "");
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

            DirectoryHelper.DeleteAllFiles(PathHelper.iTunesAlbumArtCacheDirectory);

            DevWinUI.Growl.Success(_resourceService.GetLocalizedString("ActionCompleted"));
        }

    }
}
