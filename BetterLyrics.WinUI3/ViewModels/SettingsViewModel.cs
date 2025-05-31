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
        private int _themeType;

        // Backdrop
        [ObservableProperty]
        private int _backdropType;
        [ObservableProperty]
        private bool _isCoverOverlayEnabled;
        [ObservableProperty]
        private bool _isDynamicCoverOverlay;
        [ObservableProperty]
        private int _coverOverlayOpacity;
        [ObservableProperty]
        private int _coverOverlayBlurAmount;

        // Lyrics
        [ObservableProperty]
        private int _lyricsAlignmentType;
        [ObservableProperty]
        private int _lyricsBlurAmount;

        public SettingsViewModel(ISettingsService settingsService) {

            var version = Package.Current.Id.Version;
            Version = $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";

            _settingsService = settingsService;

            PropertyChanged += SettingsViewModel_PropertyChanged;

            // Music
            LocalMusicFolders = [.. _settingsService.MusicLibraries];
            // Language
            Language = (int)_settingsService.Language;
            // Theme
            ThemeType = (int)_settingsService.Theme;
            // Backdrop
            BackdropType = (int)_settingsService.BackdropType;
            IsCoverOverlayEnabled = _settingsService.IsCoverOverlayEnabled;
            IsDynamicCoverOverlay = _settingsService.IsDynamicCoverOverlay;
            CoverOverlayOpacity = _settingsService.CoverOverlayOpacity;
            CoverOverlayBlurAmount = _settingsService.CoverOverlayBlurAmount;
            // Lyrics
            LyricsAlignmentType = (int)_settingsService.LyricsAlignmentType;
            LyricsBlurAmount = _settingsService.LyricsBlurAmount;
        }

        private void SettingsViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                // Music
                case nameof(LocalMusicFolders):
                    _settingsService.MusicLibraries = [.. LocalMusicFolders];
                    break;
                // Language
                case nameof(Language):
                    _settingsService.Language = (Models.Language)Language;
                    break;
                // Theme
                case nameof(ThemeType):
                    _settingsService.Theme = (ElementTheme)ThemeType;
                    break;
                // Backdrop
                case nameof(BackdropType):
                    _settingsService.BackdropType = (BackdropType)BackdropType;
                    break;
                case nameof(IsCoverOverlayEnabled):
                    _settingsService.IsCoverOverlayEnabled = IsCoverOverlayEnabled;
                    break;
                case nameof(IsDynamicCoverOverlay):
                    _settingsService.IsDynamicCoverOverlay = IsDynamicCoverOverlay;
                    break;
                case nameof(CoverOverlayOpacity):
                    _settingsService.CoverOverlayOpacity = CoverOverlayOpacity;
                    break;
                case nameof(CoverOverlayBlurAmount):
                    _settingsService.CoverOverlayBlurAmount = CoverOverlayBlurAmount;
                    break;
                // Lyrics
                case nameof(LyricsAlignmentType):
                    _settingsService.LyricsAlignmentType = (LyricsAlignmentType)LyricsAlignmentType;
                    break;
                case nameof(LyricsBlurAmount):
                    _settingsService.LyricsBlurAmount = LyricsBlurAmount;
                    break;
                default:
                    break;
            }
        }

    }
}
