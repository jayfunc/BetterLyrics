using System;
using System.Collections.ObjectModel;
using System.Linq;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services.Playback;
using BetterLyrics.WinUI3.Services.Settings;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using Windows.UI;

namespace BetterInAppLyrics.WinUI3.ViewModels
{
    public partial class LyricsSettingsControlViewModel : BaseViewModel
    {
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsAlignmentType LyricsAlignmentType { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsBlurAmount { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsVerticalEdgeOpacity { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial float LyricsLineSpacingFactor { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsFontSize { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsLyricsGlowEffectEnabled { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsLyricsDynamicGlowEffectEnabled { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsFontColorType LyricsFontColorType { get; set; }

        public LyricsSettingsControlViewModel(ISettingsService settingsService)
            : base(settingsService)
        {
            IsActive = true;

            LyricsAlignmentType = _settingsService.InAppLyricsAlignmentType;
            LyricsBlurAmount = _settingsService.InAppLyricsBlurAmount;
            LyricsVerticalEdgeOpacity = _settingsService.InAppLyricsVerticalEdgeOpacity;
            LyricsLineSpacingFactor = _settingsService.InAppLyricsLineSpacingFactor;
            LyricsFontSize = _settingsService.InAppLyricsFontSize;
            IsLyricsGlowEffectEnabled = _settingsService.IsInAppLyricsGlowEffectEnabled;
            IsLyricsDynamicGlowEffectEnabled =
                _settingsService.IsInAppLyricsDynamicGlowEffectEnabled;
            LyricsFontColorType = _settingsService.InAppLyricsFontColorType;
        }

        partial void OnLyricsAlignmentTypeChanged(LyricsAlignmentType value)
        {
            _settingsService.InAppLyricsAlignmentType = value;
        }

        partial void OnLyricsBlurAmountChanged(int value)
        {
            _settingsService.InAppLyricsBlurAmount = value;
        }

        partial void OnLyricsVerticalEdgeOpacityChanged(int value)
        {
            _settingsService.InAppLyricsVerticalEdgeOpacity = value;
        }

        partial void OnLyricsLineSpacingFactorChanged(float value)
        {
            _settingsService.InAppLyricsLineSpacingFactor = value;
        }

        partial void OnLyricsFontSizeChanged(int value)
        {
            _settingsService.InAppLyricsFontSize = value;
        }

        partial void OnIsLyricsGlowEffectEnabledChanged(bool value)
        {
            _settingsService.IsInAppLyricsGlowEffectEnabled = value;
        }

        partial void OnIsLyricsDynamicGlowEffectEnabledChanged(bool value)
        {
            _settingsService.IsInAppLyricsDynamicGlowEffectEnabled = value;
        }

        partial void OnLyricsFontColorTypeChanged(LyricsFontColorType value)
        {
            _settingsService.InAppLyricsFontColorType = value;
        }
    }
}
