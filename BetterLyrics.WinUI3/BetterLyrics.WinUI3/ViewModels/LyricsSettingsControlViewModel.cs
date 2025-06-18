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
        public partial LyricsFontWeight LyricsFontWeight { get; set; }

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
        public partial LyricsFontColorType LyricsFontColorType { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsGlowEffectScope LyricsGlowEffectScope { get; set; }

        public LyricsSettingsControlViewModel(ISettingsService settingsService)
            : base(settingsService)
        {
            IsActive = true;

            LyricsAlignmentType = _settingsService.LyricsAlignmentType;
            LyricsFontWeight = _settingsService.LyricsFontWeight;
            LyricsBlurAmount = _settingsService.LyricsBlurAmount;
            LyricsVerticalEdgeOpacity = _settingsService.LyricsVerticalEdgeOpacity;
            LyricsLineSpacingFactor = _settingsService.LyricsLineSpacingFactor;
            LyricsFontSize = _settingsService.LyricsFontSize;
            IsLyricsGlowEffectEnabled = _settingsService.IsLyricsGlowEffectEnabled;
            LyricsGlowEffectScope = _settingsService.LyricsGlowEffectScope;
            LyricsFontColorType = _settingsService.LyricsFontColorType;
        }

        partial void OnLyricsAlignmentTypeChanged(LyricsAlignmentType value)
        {
            _settingsService.LyricsAlignmentType = value;
        }

        partial void OnLyricsFontWeightChanged(LyricsFontWeight value)
        {
            _settingsService.LyricsFontWeight = value;
        }

        partial void OnLyricsBlurAmountChanged(int value)
        {
            _settingsService.LyricsBlurAmount = value;
        }

        partial void OnLyricsVerticalEdgeOpacityChanged(int value)
        {
            _settingsService.LyricsVerticalEdgeOpacity = value;
        }

        partial void OnLyricsLineSpacingFactorChanged(float value)
        {
            _settingsService.LyricsLineSpacingFactor = value;
        }

        partial void OnLyricsFontSizeChanged(int value)
        {
            _settingsService.LyricsFontSize = value;
        }

        partial void OnIsLyricsGlowEffectEnabledChanged(bool value)
        {
            _settingsService.IsLyricsGlowEffectEnabled = value;
        }

        partial void OnLyricsFontColorTypeChanged(LyricsFontColorType value)
        {
            _settingsService.LyricsFontColorType = value;
        }

        partial void OnLyricsGlowEffectScopeChanged(LyricsGlowEffectScope value)
        {
            _settingsService?.LyricsGlowEffectScope = value;
        }
    }
}
