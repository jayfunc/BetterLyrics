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
using DevWinUI;
using Microsoft.UI.Xaml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Windows.ApplicationModel.Core;
using Windows.Globalization;
using Windows.Media.Playback;
using Windows.System;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        [ObservableProperty]
        private bool _isRebuildingLyricsIndexDatabase = false;

        // Music
        private ObservableCollection<string> _musicLibraries;

        public ObservableCollection<string> MusicLibraries
        {
            get { return _musicLibraries; }
            set
            {
                if (_musicLibraries != null)
                {
                    _musicLibraries.CollectionChanged -= (_, _) => SaveMusicLibraries();
                }

                _musicLibraries = value;
                _musicLibraries.CollectionChanged += (_, _) => SaveMusicLibraries();
                SaveMusicLibraries();
                OnPropertyChanged();
            }
        }

        private void SaveMusicLibraries()
        {
            Set(SettingsKeys.MusicLibraries, JsonConvert.SerializeObject(MusicLibraries.ToList()));
        }

        // Language
        public int Language
        {
            get => Get(SettingsKeys.Language, SettingsDefaultValues.Language);
            set
            {
                Set(SettingsKeys.Language, value);
                switch ((Models.Language)Language)
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
        }

        private readonly MediaPlayer _mediaPlayer = new();

        private readonly IDatabaseService _databaseService;

        public string Version => AppInfo.AppVersion;

        public SettingsViewModel(IDatabaseService databaseService, ISettingsService settingsService)
            : base(settingsService)
        {
            _databaseService = databaseService;

            _musicLibraries =
            [
                .. JsonConvert.DeserializeObject<List<string>>(
                    Get(SettingsKeys.MusicLibraries, SettingsDefaultValues.MusicLibraries)!
                )!,
            ];

            _musicLibraries.CollectionChanged += (_, _) => SaveMusicLibraries();
        }

        [RelayCommand]
        private async Task RebuildLyricsIndexDatabaseAsync()
        {
            IsRebuildingLyricsIndexDatabase = true;
            await _databaseService.RebuildDatabaseAsync(MusicLibraries);
            IsRebuildingLyricsIndexDatabase = false;
            WeakReferenceMessenger.Default.Send(new ReFindSongInfoRequestedMessage());
        }

        public async Task RemoveFolderAsync(string path)
        {
            MusicLibraries.Remove(path);
            await RebuildLyricsIndexDatabaseAsync();
        }

        [RelayCommand]
        private async Task SelectAndAddFolderAsync()
        {
            var picker = new Windows.Storage.Pickers.FolderPicker();

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
