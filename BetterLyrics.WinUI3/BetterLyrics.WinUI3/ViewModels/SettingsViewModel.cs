using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Database;
using BetterLyrics.WinUI3.Services.Playback;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windows.ApplicationModel.Core;
using Windows.Globalization;
using Windows.Media.Playback;
using Windows.System;
using Windows.UI;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SettingsViewModel : ObservableRecipient
    {
        [ObservableProperty]
        public partial bool IsRebuildingLyricsIndexDatabase { get; set; } = false;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial ElementTheme ThemeType { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial BackdropType BackdropType { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial TitleBarType TitleBarType { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial ObservableCollection<string> MusicLibraries { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int CoverImageRadius { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsCoverOverlayEnabled { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsDynamicCoverOverlayEnabled { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int CoverOverlayOpacity { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int CoverOverlayBlurAmount { get; set; }

        partial void OnMusicLibrariesChanged(
            ObservableCollection<string> oldValue,
            ObservableCollection<string> newValue
        )
        {
            if (oldValue != null)
            {
                oldValue.CollectionChanged -= (_, _) =>
                    _settingsService.MusicLibraries = [.. MusicLibraries];
            }
            if (newValue != null)
            {
                newValue.CollectionChanged += (_, _) =>
                    _settingsService.MusicLibraries = [.. MusicLibraries];
            }
        }

        [ObservableProperty]
        public partial Models.Language Language { get; set; }

        partial void OnLanguageChanged(Models.Language value)
        {
            switch (value)
            {
                case Models.Language.FollowSystem:
                    ApplicationLanguages.PrimaryLanguageOverride = "";
                    break;
                case Models.Language.English:
                    ApplicationLanguages.PrimaryLanguageOverride = "en-US";
                    break;
                case Models.Language.SimplifiedChinese:
                    ApplicationLanguages.PrimaryLanguageOverride = "zh-CN";
                    break;
                case Models.Language.TraditionalChinese:
                    ApplicationLanguages.PrimaryLanguageOverride = "zh-TW";
                    break;
                default:
                    break;
            }
        }

        private readonly MediaPlayer _mediaPlayer = new();

        private readonly IDatabaseService _databaseService;
        private readonly ISettingsService _settingsService;

        public string Version { get; set; } = AppInfo.AppVersion;

        public SettingsViewModel(IDatabaseService databaseService, ISettingsService settingsService)
        {
            _databaseService = databaseService;
            _settingsService = settingsService;

            MusicLibraries = [.. _settingsService.MusicLibraries];
            Language = _settingsService.Language;
            CoverImageRadius = _settingsService.CoverImageRadius;
            ThemeType = _settingsService.ThemeType;
            BackdropType = _settingsService.BackdropType;
            TitleBarType = _settingsService.TitleBarType;

            IsCoverOverlayEnabled = _settingsService.IsCoverOverlayEnabled;
            IsDynamicCoverOverlayEnabled = _settingsService.IsDynamicCoverOverlayEnabled;
            CoverOverlayOpacity = _settingsService.CoverOverlayOpacity;
            CoverOverlayBlurAmount = _settingsService.CoverOverlayBlurAmount;
        }

        partial void OnMusicLibrariesChanged(ObservableCollection<string> value)
        {
            _settingsService.MusicLibraries = [.. value];
        }

        partial void OnThemeTypeChanged(ElementTheme value)
        {
            _settingsService.ThemeType = value;
        }

        partial void OnBackdropTypeChanged(BackdropType value)
        {
            _settingsService.BackdropType = value;
        }

        partial void OnTitleBarTypeChanged(TitleBarType value)
        {
            _settingsService.TitleBarType = value;
        }

        partial void OnCoverImageRadiusChanged(int value)
        {
            _settingsService.CoverImageRadius = value;
        }

        partial void OnIsCoverOverlayEnabledChanged(bool value)
        {
            _settingsService.IsCoverOverlayEnabled = value;
        }

        partial void OnIsDynamicCoverOverlayEnabledChanged(bool value)
        {
            _settingsService.IsDynamicCoverOverlayEnabled = value;
        }

        partial void OnCoverOverlayOpacityChanged(int value)
        {
            _settingsService.CoverOverlayOpacity = value;
        }

        partial void OnCoverOverlayBlurAmountChanged(int value)
        {
            _settingsService.CoverOverlayBlurAmount = value;
        }

        [RelayCommand]
        private async Task RebuildLyricsIndexDatabaseAsync()
        {
            IsRebuildingLyricsIndexDatabase = true;
            await _databaseService.RebuildDatabaseAsync(MusicLibraries);
            IsRebuildingLyricsIndexDatabase = false;
        }

        public async Task RemoveFolderAsync(string path)
        {
            MusicLibraries.Remove(path);
            await RebuildLyricsIndexDatabaseAsync();
        }

        [RelayCommand]
        private async Task SelectAndAddFolderAsync(UIElement sender)
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();

            picker.FileTypeFilter.Add("*");

            var hwnd = WindowNative.GetWindowHandle(WindowHelper.GetWindowForElement(sender));
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                if (MusicLibraries.Any((item) => item.StartsWith(folder.Path)))
                {
                    WeakReferenceMessenger.Default.Send(
                        new ShowNotificatonMessage(
                            new Notification(
                                App.ResourceLoader!.GetString("SettingsPagePathBeIncludedInfo")
                            )
                        )
                    );
                }
                else
                {
                    await AddFolderAsync(folder.Path);
                }
            }
        }

        private async Task AddFolderAsync(string path)
        {
            bool existed = MusicLibraries.Any((x) => x == path);
            if (existed)
            {
                WeakReferenceMessenger.Default.Send(
                    new ShowNotificatonMessage(
                        new Notification(
                            App.ResourceLoader!.GetString("SettingsPagePathExistedInfo")
                        )
                    )
                );
            }
            else
            {
                MusicLibraries.Add(path);
                await RebuildLyricsIndexDatabaseAsync();
            }
        }

        [RelayCommand]
        private async Task LaunchProjectGitHubPageAsync()
        {
            await Launcher.LaunchUriAsync(new Uri(Helper.AppInfo.GithubUrl));
        }

        private void OpenFolderInFileExplorer(string path)
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

        public void OpenMusicFolder(string path)
        {
            OpenFolderInFileExplorer(path);
        }

        [RelayCommand]
        private void RestartApp()
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
            await AddFolderAsync(AppInfo.AssetsFolder);
            _mediaPlayer.SetUriSource(new Uri(AppInfo.TestMusicPath));
            _mediaPlayer.Play();
        }

        [RelayCommand]
        private void OpenLogFolder()
        {
            OpenFolderInFileExplorer(AppInfo.LogDirectory);
        }
    }
}
