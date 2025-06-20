using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Newtonsoft.Json.Linq;
using Windows.ApplicationModel.Core;
using Windows.Globalization;
using Windows.Media.Playback;
using Windows.System;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SettingsViewModel : ObservableRecipient
    {
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
        public partial AutoStartWindowType AutoStartWindowType { get; set; }

        [ObservableProperty]
        public partial ObservableCollection<string> MusicLibraries { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial ObservableCollection<LyricsSearchProviderInfo> LyricsSearchProvidersInfo { get; set; }

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

        [ObservableProperty]
        public partial Enums.Language Language { get; set; }

        private readonly MediaPlayer _mediaPlayer = new();

        private readonly ISettingsService _settingsService;
        private readonly ILibWatcherService _libWatcherService;

        public string Version { get; set; } = AppInfo.AppVersion;

        [ObservableProperty]
        public partial object NavViewSelectedItemTag { get; set; } = "LyricsLib";

        [ObservableProperty]
        public partial Thickness RootGridMargin { get; set; } = new(0, 0, 0, 0);

        public SettingsViewModel(
            ISettingsService settingsService,
            ILibWatcherService libWatcherService,
            IPlaybackService playbackService
        )
        {
            _settingsService = settingsService;
            _libWatcherService = libWatcherService;

            RootGridMargin = new Thickness(0, _settingsService.TitleBarType.GetHeight(), 0, 0);

            MusicLibraries = [.. _settingsService.MusicLibraries];
            LyricsSearchProvidersInfo = [.. _settingsService.LyricsSearchProvidersInfo];

            Language = _settingsService.Language;
            CoverImageRadius = _settingsService.CoverImageRadius;
            ThemeType = _settingsService.ThemeType;
            BackdropType = _settingsService.BackdropType;
            TitleBarType = _settingsService.TitleBarType;

            AutoStartWindowType = _settingsService.AutoStartWindowType;

            IsCoverOverlayEnabled = _settingsService.IsCoverOverlayEnabled;
            IsDynamicCoverOverlayEnabled = _settingsService.IsDynamicCoverOverlayEnabled;
            CoverOverlayOpacity = _settingsService.CoverOverlayOpacity;
            CoverOverlayBlurAmount = _settingsService.CoverOverlayBlurAmount;
        }

        partial void OnLanguageChanged(Enums.Language value)
        {
            switch (value)
            {
                case Enums.Language.FollowSystem:
                    ApplicationLanguages.PrimaryLanguageOverride = "";
                    break;
                case Enums.Language.English:
                    ApplicationLanguages.PrimaryLanguageOverride = "en-US";
                    break;
                case Enums.Language.SimplifiedChinese:
                    ApplicationLanguages.PrimaryLanguageOverride = "zh-CN";
                    break;
                case Enums.Language.TraditionalChinese:
                    ApplicationLanguages.PrimaryLanguageOverride = "zh-TW";
                    break;
                default:
                    break;
            }
            _settingsService.Language = Language;
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
            RootGridMargin = new Thickness(0, value.GetHeight(), 0, 0);
        }

        partial void OnAutoStartWindowTypeChanged(AutoStartWindowType value)
        {
            _settingsService.AutoStartWindowType = value;
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

        public void RemoveFolderAsync(string path)
        {
            MusicLibraries.Remove(path);
            _settingsService.MusicLibraries = [.. MusicLibraries];
            _libWatcherService.UpdateWatchers([.. MusicLibraries]);
            Broadcast(MusicLibraries, MusicLibraries, nameof(MusicLibraries));
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
                if (MusicLibraries.Any((item) => folder.Path.StartsWith(item)))
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
                    AddFolderAsync(folder.Path);
                }
            }
        }

        private void AddFolderAsync(string path)
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
                _settingsService.MusicLibraries = [.. MusicLibraries];
                _libWatcherService.UpdateWatchers([.. MusicLibraries]);
                Broadcast(MusicLibraries, MusicLibraries, nameof(MusicLibraries));
            }
        }

        [RelayCommand]
        private async Task LaunchProjectGitHubPageAsync()
        {
            await Launcher.LaunchUriAsync(new Uri(AppInfo.GithubUrl));
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
        private void PlayTestingMusicTask()
        {
            AddFolderAsync(AppInfo.AssetsFolder);
            _mediaPlayer.SetUriSource(new Uri(AppInfo.TestMusicPath));
            _mediaPlayer.Play();
        }

        [RelayCommand]
        private void OpenLogFolder()
        {
            OpenFolderInFileExplorer(AppInfo.LogDirectory);
        }

        public void ToggleLyricsSearchProvider(LyricsSearchProviderInfo providerInfo)
        {
            _settingsService.LyricsSearchProvidersInfo = [.. LyricsSearchProvidersInfo];
            Broadcast(
                LyricsSearchProvidersInfo,
                LyricsSearchProvidersInfo,
                nameof(LyricsSearchProvidersInfo)
            );
        }
    }
}
