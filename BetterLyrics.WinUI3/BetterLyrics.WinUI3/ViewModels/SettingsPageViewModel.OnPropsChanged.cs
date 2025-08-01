using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Views;
using Microsoft.UI.Xaml;
using Windows.Globalization;
using Windows.UI;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SettingsPageViewModel
    {
        partial void OnDockPlacementChanged(DockPlacement value)
        {
            _settingsService.DockPlacement = value;
        }
        partial void OnLyricsScrollEasingTypeChanged(EasingType value)
        {
            _settingsService.LyricsScrollEasingType = value;
        }
        partial void OnLyricsScrollDurationChanged(int value)
        {
            _settingsService.LyricsScrollDuration = value;
        }
        partial void OnLyricsBackgroundThemeChanged(ElementTheme value)
        {
            _settingsService.LyricsBackgroundTheme = value;
        }
        partial void OnLyricsFontStrokeWidthChanged(int value)
        {
            _settingsService.LyricsFontStrokeWidth = value;
        }
        partial void OnIgnoreFullscreenWindowChanged(bool value)
        {
            _settingsService.IgnoreFullscreenWindow = value;
        }
        partial void OnSelectedTargetLanguageIndexChanged(int value)
        {
            _settingsService.SelectedTargetLanguageIndex = value;
        }
        partial void OnLibreTranslateServerChanged(string value)
        {
            _settingsService.LibreTranslateServer = value;
        }
        partial void OnLXMusicServerChanged(string value)
        {
            _settingsService.LXMusicServer = value;
        }
        partial void OnAutoStartWindowTypeChanged(AutoStartWindowType value)
        {
            _settingsService.AutoStartWindowType = value;
        }
        partial void OnAutoLockOnDesktopModeChanged(bool value)
        {
            _settingsService.AutoLockOnDesktopMode = value;
        }
        partial void OnCoverImageRadiusChanged(int value)
        {
            _settingsService.CoverImageRadius = value;
        }
        partial void OnCoverOverlayBlurAmountChanged(int value)
        {
            _settingsService.CoverOverlayBlurAmount = value;
        }
        partial void OnCoverAcrylicEffectAmountChanged(int value)
        {
            _settingsService.CoverAcrylicEffectAmount = value;
        }
        partial void OnCoverOverlayOpacityChanged(int value)
        {
            _settingsService.CoverOverlayOpacity = value;
        }
        partial void OnIsDynamicCoverOverlayEnabledChanged(bool value)
        {
            _settingsService.IsDynamicCoverOverlayEnabled = value;
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
            _settingsService.Language = Language;
        }
        partial void OnIsFanLyricsEnabledChanged(bool value)
        {
            _settingsService.IsFanLyricsEnabled = value;
        }
        partial void OnIsLyricsGlowEffectEnabledChanged(bool value)
        {
            _settingsService.IsLyricsGlowEffectEnabled = value;
        }
        partial void OnLyricsAlignmentTypeChanged(TextAlignmentType value)
        {
            _settingsService.LyricsAlignmentType = value;
        }
        partial void OnSongInfoAlignmentTypeChanged(TextAlignmentType value)
        {
            _settingsService.SongInfoAlignmentType = value;
        }
        partial void OnLyricsBlurAmountChanged(int value)
        {
            _settingsService.LyricsBlurAmount = value;
        }
        partial void OnLyricsCustomBgFontColorChanged(Color value)
        {
            _settingsService.LyricsCustomBgFontColor = value;
        }
        partial void OnLyricsCustomFgFontColorChanged(Color value)
        {
            _settingsService.LyricsCustomFgFontColor = value;
        }
        partial void OnLyricsCustomStrokeFontColorChanged(Color value)
        {
            _settingsService.LyricsCustomStrokeFontColor = value;
        }
        partial void OnLyricsBgFontColorTypeChanged(LyricsFontColorType value)
        {
            _settingsService.LyricsBgFontColorType = value;
        }
        partial void OnLyricsFgFontColorTypeChanged(LyricsFontColorType value)
        {
            _settingsService.LyricsFgFontColorType = value;
        }
        partial void OnLyricsStrokeFontColorTypeChanged(LyricsFontColorType value)
        {
            _settingsService.LyricsStrokeFontColorType = value;
        }
        partial void OnLyricsStandardFontSizeChanged(int value)
        {
            _settingsService.LyricsStandardFontSize = value;
        }
        partial void OnLyricsDockFontSizeChanged(int value)
        {
            _settingsService.LyricsDockFontSize = value;
        }
        partial void OnLyricsDesktopFontSizeChanged(int value)
        {
            _settingsService.LyricsDesktopFontSize = value;
        }
        partial void OnLyricsFontWeightChanged(LyricsFontWeight value)
        {
            _settingsService.LyricsFontWeight = value;
        }
        partial void OnLyricsGlowEffectScopeChanged(LineRenderingType value)
        {
            _settingsService.LyricsGlowEffectScope = value;
        }
        partial void OnLyricsHighlightScopeChanged(LineRenderingType value)
        {
            _settingsService.LyricsHighlightScope = value;
        }
        partial void OnLyricsLineSpacingFactorChanged(float value)
        {
            _settingsService.LyricsLineSpacingFactor = value;
        }
        partial void OnLyricsVerticalEdgeOpacityChanged(int value)
        {
            _settingsService.LyricsVerticalEdgeOpacity = value;
        }
        partial void OnTimelineSyncThresholdChanged(int value)
        {
            _settingsService.TimelineSyncThreshold = value;
        }
        partial void OnIsLyricsFloatAnimationEnabledChanged(bool value)
        {
            _settingsService.IsLyricsFloatAnimationEnabled = value;
        }
        partial void OnResetPositionOffsetOnSongChangedChanged(bool value)
        {
            _settingsService.ResetPositionOffsetOnSongChanged = value;
        }
        partial void OnLyricsBgFontOpacityChanged(int value)
        {
            _settingsService.LyricsBgFontOpacity = value;
        }
        partial void OnHideWindowWhenNotPlayingChanged(bool value)
        {
            _settingsService.HideWindowWhenNotPlaying = value;
        }
        partial void OnDockWindowHeightChanged(int value)
        {
            _settingsService.DockWindowHeight = value;
        }
        partial void OnSelectedFontFamilyIndexChanged(int value)
        {
            _settingsService.SelectedFontFamilyIndex = value;
            LyricsFontFamily = SystemFontNames[value];
        }
        partial void OnLyricsFontFamilyChanged(string value)
        {
            _settingsService.LyricsFontFamily = value;
        }
        partial void OnIsDragEverywhereEnabledChanged(bool value)
        {
            _settingsService.IsDragEverywhereEnabled = value;

            LyricsWindow? lyricsWindow = WindowHelper.GetWindowByWindowType<LyricsWindow>();
            if (lyricsWindow != null)
            {
                lyricsWindow.UpdateTitleBarArea();
            }
        }
        partial void OnIsLibreTranslateEnabledChanged(bool value)
        {
            _settingsService.IsLibreTranslateEnabled = value;
        }
        partial void OnSelectedDockMonitorDeviceNameChanged(string value)
        {
            _settingsService.DockMonitorDeviceName = value;
        }
    }
}
