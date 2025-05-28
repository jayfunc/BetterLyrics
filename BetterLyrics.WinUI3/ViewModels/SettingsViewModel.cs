using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using DevWinUI;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace BetterLyrics.WinUI3.ViewModels {
    public partial class SettingsViewModel : ObservableObject {

        [ObservableProperty]
        private ObservableCollection<string> _localMusicFolderPaths;

        [ObservableProperty]
        private string _version;

        private readonly ISettingsService _settingsService;

        public SettingsViewModel(ISettingsService settingsService) {
            var version = Package.Current.Id.Version;
            Version = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

            _settingsService = settingsService;

            LocalMusicFolderPaths = [.. JsonSerializer.Deserialize<List<string>>(_settingsService.Get(SettingsKeys.MusicLibraries, SettingsDefaultValues.MusicLibraries))];
        }


        public void AddMusicLibrary(string libraryPath) {
            LocalMusicFolderPaths.Add(libraryPath);
            _settingsService.Set(SettingsKeys.MusicLibraries, JsonSerializer.Serialize(LocalMusicFolderPaths));
        }

        public void RemoveMusicLibrary(string libraryPath) {
            LocalMusicFolderPaths.Remove(libraryPath);
            _settingsService.Set(SettingsKeys.MusicLibraries, JsonSerializer.Serialize(LocalMusicFolderPaths));
        }

    }
}
