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

        // Theme
        public ElementTheme Theme => (ElementTheme)Get(SettingsKeys.ThemeMode, SettingsDefaultValues.ThemeMode);

        public void SetTheme(ElementTheme theme) {
            Set(SettingsKeys.ThemeMode, (int)theme);
            if (App.Current is App app) {
                if (app.MainWindow?.Content is FrameworkElement mainWindowRoot) {
                    mainWindowRoot.RequestedTheme = theme;
                }
            }
        }

        public ElementTheme GetSystemTheme() {
            var uiSettings = new Windows.UI.ViewManagement.UISettings();
            var background = uiSettings.GetColorValue(Windows.UI.ViewManagement.UIColorType.Background);
            return background.R < 128 ? ElementTheme.Dark : ElementTheme.Light;
        }

        // Music folder
        public List<MusicFolder> MusicLibraries => JsonSerializer.Deserialize<List<MusicFolder>>(Get(SettingsKeys.MusicLibraries, SettingsDefaultValues.MusicLibraries));

        public void SetMusicLibraries(List<MusicFolder> musicLibraries) {
            Set(SettingsKeys.MusicLibraries, JsonSerializer.Serialize(musicLibraries));
        }

        // Language
        public Models.Language Language => (Models.Language)Get(SettingsKeys.Language, SettingsDefaultValues.Language);

        public void SetLanguage(Models.Language language) {
            Set(SettingsKeys.Language, (int)language);
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

        // SystemBackdrop
        public BackdropType BackdropType => (BackdropType)Get(SettingsKeys.BackdropType, SettingsDefaultValues.BackdropType);

        public void SetBackdropType(BackdropType backdropType) {
            Set(SettingsKeys.BackdropType, (int)backdropType);

            SystemBackdrop? systemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(backdropType);
            if (App.Current is App app) {
                if (app.MainWindow is Window mainWindow) {
                    mainWindow.SystemBackdrop = systemBackdrop;
                }
            }

        }

        // IsCoverOverlayEnabled
        public bool IsCoverOverlayEnabled => Get(SettingsKeys.IsCoverOverlayEnabled, SettingsDefaultValues.IsCoverOverlayEnabled);

        public void SetIsCoverOverlayEnabled(bool isCoverOverlayEnabled) {
            Set(SettingsKeys.IsCoverOverlayEnabled, isCoverOverlayEnabled);
        }

        // IsDynamicCoverOverlay

        public bool IsDynamicCoverOverlay => Get(SettingsKeys.IsDynamicCoverOverlay, SettingsDefaultValues.IsDynamicCoverOverlay);

        public void SetDynamicCoverOverlay(bool isDynamicCoverOverlay) {
            Set(SettingsKeys.IsDynamicCoverOverlay, isDynamicCoverOverlay);
        }

        // CoverOverlayOpacity

        public int CoverOverlayOpacity => Get(SettingsKeys.CoverOverlayOpacity, SettingsDefaultValues.CoverOverlayOpacity);

        public void SetCoverOverlayOpacity(int overlayOpacity) {
            Set(SettingsKeys.CoverOverlayOpacity, overlayOpacity);
        }

    }
}
