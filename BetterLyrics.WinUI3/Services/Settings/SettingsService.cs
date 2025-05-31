using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using DevWinUI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Globalization;
using Windows.Storage;

namespace BetterLyrics.WinUI3.Services.Settings {
    public class SettingsService : ISettingsService {

        // Theme
        public ElementTheme Theme {
            get => (ElementTheme)Get(SettingsKeys.ThemeType, SettingsDefaultValues.ThemeType);
            set {
                Set(SettingsKeys.ThemeType, (int)value);
                if (App.Current is App app) {
                    if (app.MainWindow?.Content is FrameworkElement mainWindowRoot) {
                        mainWindowRoot.RequestedTheme = value;
                    }
                }
            }
        }

        // Music
        public List<MusicFolder> MusicLibraries {
            get => JsonSerializer.Deserialize<List<MusicFolder>>(Get(SettingsKeys.MusicLibraries, SettingsDefaultValues.MusicLibraries));
            set => Set(SettingsKeys.MusicLibraries, JsonSerializer.Serialize(value));
        }

        // Language
        public Models.Language Language {
            get => (Models.Language)Get(SettingsKeys.Language, SettingsDefaultValues.Language);
            set {
                Set(SettingsKeys.Language, (int)value);
                switch (Language) {
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
        public BackdropType BackdropType {
            get => (BackdropType)Get(SettingsKeys.BackdropType, SettingsDefaultValues.BackdropType);
            set {
                Set(SettingsKeys.BackdropType, (int)value);
                SystemBackdrop? systemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(value);
                if (App.Current is App app) {
                    if (app.MainWindow is Window mainWindow) {
                        mainWindow.SystemBackdrop = systemBackdrop;
                    }
                }
            }
        }
        public bool IsCoverOverlayEnabled {
            get => Get(SettingsKeys.IsCoverOverlayEnabled, SettingsDefaultValues.IsCoverOverlayEnabled);
            set => Set(SettingsKeys.IsCoverOverlayEnabled, value);
        }
        public bool IsDynamicCoverOverlay {
            get => Get(SettingsKeys.IsDynamicCoverOverlay, SettingsDefaultValues.IsDynamicCoverOverlay);
            set => Set(SettingsKeys.IsDynamicCoverOverlay, value);
        }
        public int CoverOverlayOpacity {
            get => Get(SettingsKeys.CoverOverlayOpacity, SettingsDefaultValues.CoverOverlayOpacity);
            set => Set(SettingsKeys.CoverOverlayOpacity, value);
        }
        public int CoverOverlayBlurAmount {
            get => Get(SettingsKeys.CoverOverlayBlurAmount, SettingsDefaultValues.CoverOverlayBlurAmount);
            set => Set(SettingsKeys.CoverOverlayBlurAmount, value);
        }

        // Lyrics
        public LyricsAlignmentType LyricsAlignmentType {
            get => (LyricsAlignmentType)Get(SettingsKeys.LyricsAlignmentType, SettingsDefaultValues.LyricsAlignmentType);
            set => Set(SettingsKeys.LyricsAlignmentType, (int)value);
        }
        public int LyricsBlurAmount {
            get => Get(SettingsKeys.LyricsBlurAmount, SettingsDefaultValues.LyricsBlurAmount);
            set => Set(SettingsKeys.LyricsBlurAmount, value);
        }

        private readonly ApplicationDataContainer _localSettings;

        public SettingsService() {
            _localSettings = ApplicationData.Current.LocalSettings;
        }

        private T Get<T>(string key, T defaultValue = default) {
            if (_localSettings.Values.TryGetValue(key, out object value)) {
                return (T)Convert.ChangeType(value, typeof(T));
            }

            return defaultValue;
        }

        private void Set<T>(string key, T value) {
            _localSettings.Values[key] = value;
        }
    }
}
