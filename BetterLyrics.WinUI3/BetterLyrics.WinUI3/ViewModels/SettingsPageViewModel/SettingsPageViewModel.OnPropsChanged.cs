using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Views;
using Microsoft.UI.Xaml;
using Windows.Globalization;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels.SettingsPageViewModel
{
    public partial class SettingsPageViewModel
    {
        partial void OnDockPlacementChanged(DockPlacement value)
        {
            _settingsService.AppSettings.DockPlacement = value;
        }
        partial void OnLyricsScrollEasingTypeChanged(EasingType value)
        {
            _settingsService.AppSettings.LyricsScrollEasingType = value;
        }
        partial void OnLyricsScrollDurationChanged(int value)
        {
            _settingsService.AppSettings.LyricsScrollDuration = value;
        }
        partial void OnLyricsScrollTopDurationChanged(int value)
        {
            _settingsService.AppSettings.LyricsScrollTopDuration = value;
        }
        partial void OnLyricsScrollBottomDurationChanged(int value)
        {
            _settingsService.AppSettings.LyricsScrollBottomDuration = value;
        }
        partial void OnLyricsBackgroundThemeChanged(ElementTheme value)
        {
            _settingsService.AppSettings.LyricsBackgroundTheme = value;
        }
        partial void OnLyricsFontStrokeWidthChanged(int value)
        {
            _settingsService.AppSettings.LyricsFontStrokeWidth = value;
        }
        partial void OnIgnoreFullscreenWindowChanged(bool value)
        {
            _settingsService.AppSettings.IgnoreFullscreenWindow = value;
        }
        partial void OnSelectedTargetLanguageIndexChanged(int value)
        {
            _settingsService.AppSettings.SelectedTargetLanguageIndex = value;
        }
        partial void OnLibreTranslateServerChanged(string value)
        {
            _settingsService.AppSettings.LibreTranslateServer = value;
        }
        partial void OnLXMusicServerChanged(string value)
        {
            _settingsService.AppSettings.LXMusicServer = value;
        }
        partial void OnAutoStartWindowTypeChanged(AutoStartWindowType value)
        {
            _settingsService.AppSettings.AutoStartWindowType = value;
        }
        partial void OnAutoLockOnDesktopModeChanged(bool value)
        {
            _settingsService.AppSettings.AutoLockOnDesktopMode = value;
        }
        partial void OnCoverImageRadiusChanged(int value)
        {
            _settingsService.AppSettings.CoverImageRadius = value;
        }
        partial void OnCoverOverlayBlurAmountChanged(int value)
        {
            _settingsService.AppSettings.CoverOverlayBlurAmount = value;
        }
        partial void OnCoverAcrylicEffectAmountChanged(int value)
        {
            _settingsService.AppSettings.CoverAcrylicEffectAmount = value;
        }
        partial void OnCoverOverlayOpacityChanged(int value)
        {
            _settingsService.AppSettings.CoverOverlayOpacity = value;
        }
        partial void OnCoverOverlaySpeedChanged(int value)
        {
            _settingsService.AppSettings.CoverOverlaySpeed = value;
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
                case Enums.Language.Japanese:
                    ApplicationLanguages.PrimaryLanguageOverride = "ja-JP";
                    break;
                case Enums.Language.Korean:
                    ApplicationLanguages.PrimaryLanguageOverride = "ko-KR";
                    break;
                default:
                    break;
            }
            _settingsService.AppSettings.Language = Language;
        }
        partial void OnIsFanLyricsEnabledChanged(bool value)
        {
            _settingsService.AppSettings.IsFanLyricsEnabled = value;
        }
        partial void OnIsLyricsGlowEffectEnabledChanged(bool value)
        {
            _settingsService.AppSettings.IsLyricsGlowEffectEnabled = value;
        }
        partial void OnLyricsAlignmentTypeChanged(TextAlignmentType value)
        {
            _settingsService.AppSettings.LyricsAlignmentType = value;
        }
        partial void OnSongInfoAlignmentTypeChanged(TextAlignmentType value)
        {
            _settingsService.AppSettings.SongInfoAlignmentType = value;
        }
        partial void OnLyricsBlurAmountChanged(int value)
        {
            _settingsService.AppSettings.LyricsBlurAmount = value;
        }
        partial void OnLyricsCustomBgFontColorChanged(Color value)
        {
            _settingsService.AppSettings.LyricsCustomBgFontColor = value;
        }
        partial void OnLyricsCustomFgFontColorChanged(Color value)
        {
            _settingsService.AppSettings.LyricsCustomFgFontColor = value;
        }
        partial void OnLyricsCustomStrokeFontColorChanged(Color value)
        {
            _settingsService.AppSettings.LyricsCustomStrokeFontColor = value;
        }
        partial void OnLyricsBgFontColorTypeChanged(LyricsFontColorType value)
        {
            _settingsService.AppSettings.LyricsBgFontColorType = value;
        }
        partial void OnLyricsFgFontColorTypeChanged(LyricsFontColorType value)
        {
            _settingsService.AppSettings.LyricsFgFontColorType = value;
        }
        partial void OnLyricsStrokeFontColorTypeChanged(LyricsFontColorType value)
        {
            _settingsService.AppSettings.LyricsStrokeFontColorType = value;
        }
        partial void OnLyricsStandardFontSizeChanged(int value)
        {
            _settingsService.AppSettings.LyricsStandardFontSize = value;
        }
        partial void OnLyricsDockFontSizeChanged(int value)
        {
            _settingsService.AppSettings.LyricsDockFontSize = value;
        }
        partial void OnLyricsDesktopFontSizeChanged(int value)
        {
            _settingsService.AppSettings.LyricsDesktopFontSize = value;
        }
        partial void OnLyricsFontWeightChanged(LyricsFontWeight value)
        {
            _settingsService.AppSettings.LyricsFontWeight = value;
        }
        partial void OnLyricsGlowEffectScopeChanged(LineRenderingType value)
        {
            _settingsService.AppSettings.LyricsGlowEffectScope = value;
        }
        partial void OnLyricsHighlightScopeChanged(LineRenderingType value)
        {
            _settingsService.AppSettings.LyricsHighlightScope = value;
        }
        partial void OnLyricsLineSpacingFactorChanged(double value)
        {
            _settingsService.AppSettings.LyricsLineSpacingFactor = value;
        }
        partial void OnLyricsVerticalEdgeOpacityChanged(int value)
        {
            _settingsService.AppSettings.LyricsVerticalEdgeOpacity = value;
        }
        partial void OnIsLyricsFloatAnimationEnabledChanged(bool value)
        {
            _settingsService.AppSettings.IsLyricsFloatAnimationEnabled = value;
        }
        partial void OnLyricsBgFontOpacityChanged(int value)
        {
            _settingsService.AppSettings.LyricsBgFontOpacity = value;
        }
        partial void OnHideWindowWhenNotPlayingChanged(bool value)
        {
            _settingsService.AppSettings.HideWindowWhenNotPlaying = value;
        }
        partial void OnDockWindowHeightChanged(int value)
        {
            _settingsService.AppSettings.DockWindowHeight = value;
        }
        partial void OnSelectedFontFamilyIndexChanged(int value)
        {
            _settingsService.AppSettings.SelectedFontFamilyIndex = value;
            LyricsFontFamily = SystemFontNames[value];
        }
        partial void OnLyricsFontFamilyChanged(string value)
        {
            _settingsService.AppSettings.LyricsFontFamily = value;
        }
        partial void OnIsDragEverywhereEnabledChanged(bool value)
        {
            _settingsService.AppSettings.IsDragEverywhereEnabled = value;

            LyricsWindow? lyricsWindow = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (lyricsWindow != null)
            {
                lyricsWindow.UpdateTitleBarArea();
            }
        }
        partial void OnIsLibreTranslateEnabledChanged(bool value)
        {
            _settingsService.AppSettings.IsLibreTranslateEnabled = value;
        }
        partial void OnSelectedDockMonitorDeviceNameChanged(string value)
        {
            _settingsService.AppSettings.DockMonitorDeviceName = value;
        }
        partial void OnLyricsTranslationSeparatorChanged(string value)
        {
            _settingsService.AppSettings.LyricsTranslationSeparator = value;
        }
    }
}
