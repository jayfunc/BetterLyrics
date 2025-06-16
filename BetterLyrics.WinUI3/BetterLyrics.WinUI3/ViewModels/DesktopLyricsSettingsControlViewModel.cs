using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Playback;
using BetterLyrics.WinUI3.Services.Settings;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class DesktopLyricsSettingsControlViewModel
        : BaseLyricsSettingsControlViewModel,
            ILyricsSettingsControlViewModel
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

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsFontSelectedAccentColorIndex { get; set; }

        public DesktopLyricsSettingsControlViewModel(
            ISettingsService settingsService,
            IPlaybackService playbackService
        )
            : base(settingsService, playbackService)
        {
            LyricsAlignmentType = _settingsService.DesktopLyricsAlignmentType;
            LyricsBlurAmount = _settingsService.DesktopLyricsBlurAmount;
            LyricsVerticalEdgeOpacity = _settingsService.DesktopLyricsVerticalEdgeOpacity;
            LyricsLineSpacingFactor = _settingsService.DesktopLyricsLineSpacingFactor;
            LyricsFontSize = _settingsService.DesktopLyricsFontSize;
            IsLyricsGlowEffectEnabled = _settingsService.IsDesktopLyricsGlowEffectEnabled;
            IsLyricsDynamicGlowEffectEnabled =
                _settingsService.IsDesktopLyricsDynamicGlowEffectEnabled;
            LyricsFontColorType = _settingsService.DesktopLyricsFontColorType;
            LyricsFontSelectedAccentColorIndex =
                _settingsService.DesktopLyricsFontSelectedAccentColorIndex;
        }

        partial void OnLyricsAlignmentTypeChanged(LyricsAlignmentType value)
        {
            _settingsService.DesktopLyricsAlignmentType = value;
        }

        partial void OnLyricsBlurAmountChanged(int value)
        {
            _settingsService.DesktopLyricsBlurAmount = value;
        }

        partial void OnLyricsVerticalEdgeOpacityChanged(int value)
        {
            _settingsService.DesktopLyricsVerticalEdgeOpacity = value;
        }

        partial void OnLyricsLineSpacingFactorChanged(float value)
        {
            _settingsService.DesktopLyricsLineSpacingFactor = value;
        }

        partial void OnLyricsFontSizeChanged(int value)
        {
            _settingsService.DesktopLyricsFontSize = value;
        }

        partial void OnIsLyricsGlowEffectEnabledChanged(bool value)
        {
            _settingsService.IsDesktopLyricsGlowEffectEnabled = value;
        }

        partial void OnIsLyricsDynamicGlowEffectEnabledChanged(bool value)
        {
            _settingsService.IsDesktopLyricsDynamicGlowEffectEnabled = value;
        }

        partial void OnLyricsFontColorTypeChanged(LyricsFontColorType value)
        {
            _settingsService.DesktopLyricsFontColorType = value;
        }

        partial void OnLyricsFontSelectedAccentColorIndexChanged(int value)
        {
            _settingsService.DesktopLyricsFontSelectedAccentColorIndex = value;
        }
    }
}
