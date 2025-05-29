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
using Windows.Globalization;

namespace BetterLyrics.WinUI3.ViewModels {
    public partial class SettingsViewModel : ObservableObject {

        private readonly ISettingsService _settingsService;

        [ObservableProperty]
        private string _version;

        // Settings related

        [ObservableProperty]
        private ObservableCollection<MusicFolder> _localMusicFolders;

        [ObservableProperty]
        private int _language;

        [ObservableProperty]
        private int _theme;

        [ObservableProperty]
        private int _backdropType;

        [ObservableProperty]
        private bool _isCoverOverlayEnabled;

        [ObservableProperty]
        private bool _isDynamicCoverOverlay;

        [ObservableProperty]
        private int _coverOverlayOpacity;

        public SettingsViewModel(ISettingsService settingsService) {
            var version = Package.Current.Id.Version;
            Version = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

            _settingsService = settingsService;

            LocalMusicFolders = [.. _settingsService.MusicLibraries];
            Language = (int)_settingsService.Language;
            Theme = (int)_settingsService.Theme;
            BackdropType = (int)_settingsService.BackdropType;
            IsCoverOverlayEnabled = _settingsService.IsCoverOverlayEnabled;
            IsDynamicCoverOverlay = _settingsService.IsDynamicCoverOverlay;
            CoverOverlayOpacity = _settingsService.CoverOverlayOpacity;
        }

        public void AddMusicLibrary(string libraryPath) {
            LocalMusicFolders.Add(new MusicFolder(libraryPath));
            _settingsService.SetMusicLibraries([.. LocalMusicFolders]);
        }

        public void RemoveMusicLibrary(MusicFolder source) {
            LocalMusicFolders.Remove(source);
            _settingsService.SetMusicLibraries([.. LocalMusicFolders]);
        }

        public void SetLanguage() {
            _settingsService.SetLanguage((Models.Language)Language);
        }

        public void SetTheme() {
            _settingsService.SetTheme((ElementTheme)Theme);
        }

        public void SetBackdropType() {
            _settingsService.SetBackdropType((BackdropType)BackdropType);
        }

        public void SetIsCoverOverlayEnabled(bool value) {
            _settingsService.SetIsCoverOverlayEnabled(value);
        }

        public void SetDynamicCoverOverlay(bool value) {
            _settingsService.SetDynamicCoverOverlay(value);
        }

        public void SetCoverOverlayOpacity() {
            _settingsService.SetCoverOverlayOpacity(CoverOverlayOpacity);
        }

    }
}
