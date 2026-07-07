using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Text.RegularExpressions;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;

namespace BetterLyrics.Core.ViewModels;

public partial class AboutControlViewModel : BaseViewModel
{
    private readonly IGlobalToastProvider _globalToastProvider;
    private readonly ILocalizationService _localizationService;
    private readonly ILyricsCacheService _lyricsCacheService;
    private readonly ISettingsService _settingsService;
    private readonly IWindowManagerProvider _windowManagerProvider;
    private readonly IAssetReaderProvider _assetReaderProvider;
    private readonly ILauncherProvider _launcherProvider;
    private readonly IFilePickerProvider _filePickerProvider;

    public AboutControlViewModel(ISettingsService settingsService, ILyricsCacheService lyricsCacheService,
        IAppUpdateService appUpdateService, ILocalizationService localizationService,
        IGlobalToastProvider globalToastProvider, IWindowManagerProvider windowManagerProvider,
        IAssetReaderProvider assetReaderProvider, ILauncherProvider launcherProvider,
        IFilePickerProvider filePickerProvider)
    {
        _settingsService = settingsService;
        _lyricsCacheService = lyricsCacheService;
        _localizationService = localizationService;
        _globalToastProvider = globalToastProvider;
        _windowManagerProvider = windowManagerProvider;
        _assetReaderProvider = assetReaderProvider;
        _launcherProvider = launcherProvider;
        _filePickerProvider = filePickerProvider;

        AppUpdateService = appUpdateService;

        AppSettings = _settingsService.AppSettings;
        _ = LoadContributorsAsync();
        _ = LoadDonorsAsync();
    }

    [ObservableProperty] public partial IAppUpdateService AppUpdateService { get; set; }

    [ObservableProperty] public partial AppSettings AppSettings { get; set; }

    public ObservableCollection<Contributor> Contributors { get; set; } = new();
    public ObservableCollection<Donor> Donors { get; set; } = new();

    private async Task LoadContributorsAsync()
    {
        var lines = await _assetReaderProvider.ReadAllLinesAsync("Contributors.csv");

        for (var i = 1; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            if (parts.Length >= 4)
                Contributors.Add(new Contributor
                {
                    Header = parts[0].Trim('"', ' '),
                    AvatarSource = parts[1].Trim('"', ' '),
                    Badges = parts[2].Trim('"', ' '),
                    Description = parts[3].Trim('"', ' ')
                });
        }
    }

    private async Task LoadDonorsAsync()
    {
        var lines = await _assetReaderProvider.ReadAllLinesAsync("Donors.csv");

        for (var i = 1; i < lines.Count; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            var parts = Regex.Split(line, ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)");

            if (parts.Length >= 2)
                Donors.Add(new Donor
                {
                    Date = parts[0].Trim('"', ' '),
                    PatronName = parts[1].Trim('"', ' ')
                });
        }

        Donors.Add(new Donor
            { PatronName = _localizationService.GetLocalizedString("SettingsPageUserWhoPurchased") });
    }

    [RelayCommand]
    private async Task LaunchProjectGitHubPageAsync()
    {
        await _launcherProvider.LaunchUriAsync(new Uri(Link.BetterLyricsGitHub));
    }

    [RelayCommand]
    private async Task OpenCacheFolderAsync()
    {
        await _launcherProvider.LaunchFolderPathAsync(PathHelper.CacheFolderPath);
    }

    [RelayCommand]
    private async Task OpenSettingsFolderAsync()
    {
        await _launcherProvider.LaunchFolderPathAsync(PathHelper.LocalFolderPath);
    }

    [RelayCommand]
    private async Task ImportSettingsAsync()
    {
        var (_, filePath) = await _filePickerProvider.PickSingleFileAsync([".zip"], WindowType.SettingsWindow);

        if (filePath != null)
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();

                SqliteConnection.ClearAllPools();

                var tempExtractPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
                Directory.CreateDirectory(tempExtractPath);

                await using (var stream = File.OpenRead(filePath))
                {
                    await ZipFile.ExtractToDirectoryAsync(stream, tempExtractPath);
                }

                DirectoryHelper.CopyDirectory(tempExtractPath, PathHelper.LocalFolderPath, true);

                Directory.Delete(tempExtractPath, true);

                _windowManagerProvider.RestartApp();
            }
            catch (Exception ex)
            {
                _globalToastProvider.Show("ImportSettingsFailed", ex.Message, MessageSeverity.Error);
            }
    }

    [RelayCommand]
    private async Task ExportSettingsAsync()
    {
        try
        {
            var suggestedFileName =
                $"{Core.Constants.App.AppName}_{_settingsService.AppSettings.Version}_{DateTime.Now:yyyyMMdd_HHmmss}";
            IDictionary<string, IList<string>> fileTypeChoices = new Dictionary<string, IList<string>>
            {
                { "Zip Archive", new List<string> { ".zip" } }
            };

            var (_, filePath) =
                await _filePickerProvider.PickSaveFileAsync(fileTypeChoices,
                    suggestedFileName, WindowType.SettingsWindow);
            if (filePath == null) return;

            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            DirectoryHelper.CopyDirectory(PathHelper.LocalFolderPath, tempDir, true);

            var tempZipPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".zip");

            await ZipFile.CreateFromDirectoryAsync(tempDir, tempZipPath);

            await using (var sourceStream = File.OpenRead(tempZipPath))
            await using (var destStream = File.OpenWrite(filePath))
            {
                await sourceStream.CopyToAsync(destStream);
                destStream.SetLength(sourceStream.Length);
            }

            Directory.Delete(tempDir, true);
            File.Delete(tempZipPath);

            _globalToastProvider.Show("ExportSettingsSuccess", null, MessageSeverity.Success);
        }
        catch (Exception ex)
        {
            _globalToastProvider.Show("Error", ex.Message, MessageSeverity.Error);
        }
    }

    [RelayCommand]
    private async Task ClearCacheFilesAsync()
    {
        await _lyricsCacheService.ClearCacheAsync();

        DirectoryHelper.DeleteAllFiles(PathHelper.LogDirectory);
        DirectoryHelper.DeleteAllFiles(PathHelper.LyricsCacheDirectory);
        DirectoryHelper.DeleteAllFiles(PathHelper.iTunesAlbumArtCacheDirectory);

        _globalToastProvider.Show("ActionCompleted", null, MessageSeverity.Success);
    }

    [RelayCommand]
    private async Task OpenAppStorePageAsync()
    {
        await _launcherProvider.LaunchUriAsync(new Uri(Link.StorePage));
    }

    [RelayCommand]
    private async Task CheckAppUpdateAsync()
    {
        await AppUpdateService.UpdateAvailabilityAsync();
    }
}