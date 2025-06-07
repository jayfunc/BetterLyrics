using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using BetterLyrics.WinUI3.Messages;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using DevWinUI;
using Microsoft.UI.Xaml;
using Newtonsoft.Json;
using Windows.Globalization;
using Windows.Storage;

namespace BetterLyrics.WinUI3.Services.Settings
{
    public partial class SettingsService : ObservableObject
    {
        private readonly ApplicationDataContainer _localSettings;

        public SettingsService()
        {
            _localSettings = ApplicationData.Current.LocalSettings;

            _musicLibraries =
            [
                .. JsonConvert.DeserializeObject<List<string>>(
                    Get(SettingsKeys.MusicLibraries, SettingsDefaultValues.MusicLibraries)!
                )!,
            ];

            _musicLibraries.CollectionChanged += (_, _) => SaveMusicLibraries();
        }

        public bool IsFirstRun
        {
            get => Get(SettingsKeys.IsFirstRun, SettingsDefaultValues.IsFirstRun);
            set => Set(SettingsKeys.IsFirstRun, value);
        }

        [ObservableProperty]
        private bool _isRebuildingLyricsIndexDatabase = false;

        [ObservableProperty]
        private bool _isImmersiveMode = false;

        // Theme
        public int Theme
        {
            get => Get(SettingsKeys.ThemeType, SettingsDefaultValues.ThemeType);
            set
            {
                Set(SettingsKeys.ThemeType, value);
                WeakReferenceMessenger.Default.Send(new ThemeChangedMessage((ElementTheme)value));
            }
        }

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

        // Backdrop
        public int BackdropType
        {
            get => Get(SettingsKeys.BackdropType, SettingsDefaultValues.BackdropType);
            set
            {
                Set(SettingsKeys.BackdropType, value);
                WeakReferenceMessenger.Default.Send(
                    new SystemBackdropChangedMessage((BackdropType)value)
                );
            }
        }
        public bool IsCoverOverlayEnabled
        {
            get =>
                Get(
                    SettingsKeys.IsCoverOverlayEnabled,
                    SettingsDefaultValues.IsCoverOverlayEnabled
                );
            set => Set(SettingsKeys.IsCoverOverlayEnabled, value);
        }
        public bool IsDynamicCoverOverlay
        {
            get =>
                Get(
                    SettingsKeys.IsDynamicCoverOverlay,
                    SettingsDefaultValues.IsDynamicCoverOverlay
                );
            set => Set(SettingsKeys.IsDynamicCoverOverlay, value);
        }
        public int CoverOverlayOpacity
        {
            get => Get(SettingsKeys.CoverOverlayOpacity, SettingsDefaultValues.CoverOverlayOpacity);
            set => Set(SettingsKeys.CoverOverlayOpacity, value);
        }
        public int CoverOverlayBlurAmount
        {
            get =>
                Get(
                    SettingsKeys.CoverOverlayBlurAmount,
                    SettingsDefaultValues.CoverOverlayBlurAmount
                );
            set => Set(SettingsKeys.CoverOverlayBlurAmount, value);
        }

        // Title bar
        public int TitleBarType
        {
            get => Get(SettingsKeys.TitleBarType, SettingsDefaultValues.TitleBarType);
            set => Set(SettingsKeys.TitleBarType, value);
        }

        // Album art
        public int CoverImageRadius
        {
            get => Get(SettingsKeys.CoverImageRadius, SettingsDefaultValues.CoverImageRadius);
            set => Set(SettingsKeys.CoverImageRadius, value);
        }

        // Lyrics
        public int LyricsAlignmentType
        {
            get => Get(SettingsKeys.LyricsAlignmentType, SettingsDefaultValues.LyricsAlignmentType);
            set => Set(SettingsKeys.LyricsAlignmentType, value);
        }
        public int LyricsBlurAmount
        {
            get => Get(SettingsKeys.LyricsBlurAmount, SettingsDefaultValues.LyricsBlurAmount);
            set => Set(SettingsKeys.LyricsBlurAmount, value);
        }
        public int LyricsVerticalEdgeOpacity
        {
            get =>
                Get(
                    SettingsKeys.LyricsVerticalEdgeOpacity,
                    SettingsDefaultValues.LyricsVerticalEdgeOpacity
                );
            set => Set(SettingsKeys.LyricsVerticalEdgeOpacity, value);
        }
        public float LyricsLineSpacingFactor
        {
            get =>
                Get(
                    SettingsKeys.LyricsLineSpacingFactor,
                    SettingsDefaultValues.LyricsLineSpacingFactor
                );
            set => Set(SettingsKeys.LyricsLineSpacingFactor, value);
        }
        public int LyricsFontSize
        {
            get => Get(SettingsKeys.LyricsFontSize, SettingsDefaultValues.LyricsFontSize);
            set => Set(SettingsKeys.LyricsFontSize, value);
        }
        public bool IsLyricsGlowEffectEnabled
        {
            get =>
                Get(
                    SettingsKeys.IsLyricsGlowEffectEnabled,
                    SettingsDefaultValues.IsLyricsGlowEffectEnabled
                );
            set => Set(SettingsKeys.IsLyricsGlowEffectEnabled, value);
        }
        public bool IsLyricsDynamicGlowEffectEnabled
        {
            get =>
                Get(
                    SettingsKeys.IsLyricsDynamicGlowEffectEnabled,
                    SettingsDefaultValues.IsLyricsDynamicGlowEffectEnabled
                );
            set => Set(SettingsKeys.IsLyricsDynamicGlowEffectEnabled, value);
        }
        public int LyricsFontColorType
        {
            get => Get(SettingsKeys.LyricsFontColorType, SettingsDefaultValues.LyricsFontColorType);
            set => Set(SettingsKeys.LyricsFontColorType, value);
        }
        public int LyricsFontSelectedAccentColorIndex
        {
            get =>
                Get(
                    SettingsKeys.LyricsFontSelectedAccentColorIndex,
                    SettingsDefaultValues.LyricsFontSelectedAccentColorIndex
                );
            set
            {
                if (value >= 0)
                    Set(SettingsKeys.LyricsFontSelectedAccentColorIndex, value);
            }
        }

        //Notification
        public bool NeverShowEnterFullScreenMessage
        {
            get =>
                Get(
                    SettingsKeys.NeverShowEnterFullScreenMessage,
                    SettingsDefaultValues.NeverShowEnterFullScreenMessage
                );
            set => Set(SettingsKeys.NeverShowEnterFullScreenMessage, value);
        }
        public bool NeverShowEnterImmersiveModeMessage
        {
            get =>
                Get(
                    SettingsKeys.NeverShowEnterImmersiveModeMessage,
                    SettingsDefaultValues.NeverShowEnterImmersiveModeMessage
                );
            set => Set(SettingsKeys.NeverShowEnterImmersiveModeMessage, value);
        }

        // Utils

        private T? Get<T>(string key, T? defaultValue = default)
        {
            if (_localSettings.Values.TryGetValue(key, out object? value))
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }

            return defaultValue;
        }

        private void Set<T>(string key, T value, [CallerMemberName] string? propertyName = null)
        {
            _localSettings.Values[key] = value;
            OnPropertyChanged(propertyName);
        }
    }
}
