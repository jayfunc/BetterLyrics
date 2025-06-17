using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Playback;
using BetterLyrics.WinUI3.Services.Settings;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterInAppLyrics.WinUI3.ViewModels
{
    public partial class InAppLyricsSettingsControlViewModel : BaseLyricsSettingsControlViewModel
    {
        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public override partial LyricsAlignmentType LyricsAlignmentType { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public override partial int LyricsBlurAmount { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public override partial int LyricsVerticalEdgeOpacity { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public override partial float LyricsLineSpacingFactor { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public override partial int LyricsFontSize { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public override partial bool IsLyricsGlowEffectEnabled { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public override partial bool IsLyricsDynamicGlowEffectEnabled { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public override partial LyricsFontColorType LyricsFontColorType { get; set; }

        public InAppLyricsSettingsControlViewModel(
            ISettingsService settingsService,
            IPlaybackService playbackService
        )
            : base(settingsService, playbackService)
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
