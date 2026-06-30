using BetterLyrics.Avalonia.Helpers;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace BetterLyrics.Avalonia.ViewModels
{
    public partial class AboutControlViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly ILyricsCacheService _lyricsCacheService;
        private readonly ILocalizationService _localizationService;

        [ObservableProperty] public partial IAppUpdateService AppUpdateService { get; set; }

        [ObservableProperty] public partial AppSettings AppSettings { get; set; }

        public ObservableCollection<Contributor> Contributors { get; set; } = new();
        public ObservableCollection<Donor> Donors { get; set; } = new();

        public AboutControlViewModel(ISettingsService settingsService, ILyricsCacheService lyricsCacheService, IAppUpdateService appUpdateService, ILocalizationService localizationService)
        {
            _settingsService = settingsService;
            _lyricsCacheService = lyricsCacheService;
            _localizationService = localizationService;
            AppUpdateService = appUpdateService;

            AppSettings = _settingsService.AppSettings;
            _ = LoadContributorsAsync();
            _ = LoadDonorsAsync();
        }

        private async Task LoadContributorsAsync()
        {
            //var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/contributors.csv"));
            //var lines = await FileIO.ReadLinesAsync(file);

            //for (int i = 1; i < lines.Count; i++)
            //{
            //    var line = lines[i];
            //    if (string.IsNullOrWhiteSpace(line)) continue;

            //    var parts = Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            //    if (parts.Length >= 4)
            //    {
            //        Contributors.Add(new Contributor
            //        {
            //            Header = parts[0].Trim('"', ' '),
            //            AvatarSource = parts[1].Trim('"', ' '),
            //            Badges = parts[2].Trim('"', ' '),
            //            Description = parts[3].Trim('"', ' ')
            //        });
            //    }
            //}
        }

        private async Task LoadDonorsAsync()
        {
            //var file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/Donors.csv"));
            //var lines = await FileIO.ReadLinesAsync(file);

            //for (int i = 1; i < lines.Count; i++)
            //{
            //    var line = lines[i];
            //    if (string.IsNullOrWhiteSpace(line)) continue;

            //    var parts = Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            //    if (parts.Length >= 2)
            //    {
            //        Donors.Add(new Donor
            //        {
            //            Date = parts[0].Trim('"', ' '),
            //            PatronName = parts[1].Trim('"', ' ')
            //        });
            //    }
            //}

            //Donors.Add(new Donor() { PatronName = _localizationService.GetLocalizedString("SettingsPageUserWhoPurchased") });
        }

        [RelayCommand]
        private static async Task LaunchProjectGitHubPageAsync()
        {
            //await Windows.System.Launcher.LaunchUriAsync(new Uri(Link.BetterLyricsGitHub));
        }

        [RelayCommand]
        private static async Task OpenCacheFolderAsync()
        {
            //await Windows.System.Launcher.LaunchFolderPathAsync(PathHelper.CacheFolderPath);
        }

        [RelayCommand]
        private static async Task OpenSettingsFolderAsync()
        {
            //await Windows.System.Launcher.LaunchFolderPathAsync(PathHelper.LocalFolderPath);
        }

        [RelayCommand]
        private async Task ImportSettingsAsync()
        {
            //var file = await PickerHelper.PickSingleFileAsync<SettingsWindow>([".zip"]);

            //if (file != null)
            //{
            //    try
            //    {
            //        GC.Collect();
            //        GC.WaitForPendingFinalizers();

            //        SqliteConnection.ClearAllPools();

            //        string tempExtractPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            //        Directory.CreateDirectory(tempExtractPath);

            //        using (var stream = await file.OpenStreamForReadAsync())
            //        {
            //            ZipFile.ExtractToDirectory(stream, tempExtractPath);
            //        }

            //        DirectoryHelper.CopyDirectory(tempExtractPath, PathHelper.LocalFolderPath, true);

            //        Directory.Delete(tempExtractPath, true);

            //        WindowHook.RestartApp();
            //    }
            //    catch (Exception ex)
            //    {
            //        GlobalToastManager.Show("ImportSettingsFailed", ex.Message, MessageSeverity.Error);
            //    }
            //}
        }

        [RelayCommand]
        private async Task ExportSettingsAsync()
        {
            //try
            //{
            //    var suggestedFileName = $"{Core.Constants.App.AppName}_{_settingsService.AppSettings.Version}_{DateTime.Now:yyyyMMdd_HHmmss}";
            //    IDictionary<string, IList<string>> fileTypeChoices = new Dictionary<string, IList<string>>()
            //    {
            //        { "Zip Archive", new List<string>() { ".zip" } }
            //    };

            //    var destinationFile = await PickerHelper.PickSaveFileAsync<SettingsWindow>(fileTypeChoices, suggestedFileName);
            //    if (destinationFile == null) return;

            //    string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            //    Directory.CreateDirectory(tempDir);

            //    DirectoryHelper.CopyDirectory(PathHelper.LocalFolderPath, tempDir, true);

            //    string tempZipPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".zip");

            //    ZipFile.CreateFromDirectory(tempDir, tempZipPath);

            //    using (var sourceStream = File.OpenRead(tempZipPath))
            //    using (var destStream = await destinationFile.OpenStreamForWriteAsync())
            //    {
            //        sourceStream.CopyTo(destStream);
            //        destStream.SetLength(sourceStream.Length);
            //    }

            //    Directory.Delete(tempDir, true);
            //    File.Delete(tempZipPath);

            //    GlobalToastManager.Show("ExportSettingsSuccess", null, MessageSeverity.Success);
            //}
            //catch (Exception ex)
            //{
            //    GlobalToastManager.Show("Error", ex.Message, MessageSeverity.Error);
            //}
        }

        [RelayCommand]
        private async Task ClearCacheFilesAsync()
        {
            await _lyricsCacheService.ClearCacheAsync();

            DirectoryHelper.DeleteAllFiles(PathHelper.LogDirectory);
            DirectoryHelper.DeleteAllFiles(PathHelper.LyricsCacheDirectory);
            DirectoryHelper.DeleteAllFiles(PathHelper.iTunesAlbumArtCacheDirectory);

            GlobalToastManager.Show("ActionCompleted", null, MessageSeverity.Success);
        }

        [RelayCommand]
        private static async Task OpenAppStorePageAsync()
        {
            //await Launcher.LaunchUriAsync(new Uri(Link.StorePage));
        }

        [RelayCommand]
        private async Task CheckAppUpdateAsync()
        {
            await AppUpdateService.UpdateAvailabilityAsync();
        }

        [RelayCommand]
        private async Task OpenUriAsync()
        {

        }

    }

}
