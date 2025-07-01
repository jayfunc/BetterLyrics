// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.UI;

namespace BetterInAppLyrics.WinUI3.ViewModels
{
    public partial class LyricsSettingsControlViewModel : BaseViewModel
    {
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
            IsFanLyricsEnabled = _settingsService.IsFanLyricsEnabled;
            LyricsFontColorType = _settingsService.LyricsFontColorType;
            LyricsCustomFontColor = _settingsService.LyricsCustomFontColor;
        }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsFanLyricsEnabled { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsLyricsGlowEffectEnabled { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsAlignmentType LyricsAlignmentType { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsBlurAmount { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial Color LyricsCustomFontColor { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsFontColorType LyricsFontColorType { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsFontSize { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsFontWeight LyricsFontWeight { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LineRenderingType LyricsGlowEffectScope { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial float LyricsLineSpacingFactor { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int LyricsVerticalEdgeOpacity { get; set; }

        partial void OnIsFanLyricsEnabledChanged(bool value)
        {
            _settingsService.IsFanLyricsEnabled = value;
        }

        partial void OnIsLyricsGlowEffectEnabledChanged(bool value)
        {
            _settingsService.IsLyricsGlowEffectEnabled = value;
        }

        partial void OnLyricsAlignmentTypeChanged(LyricsAlignmentType value)
        {
            _settingsService.LyricsAlignmentType = value;
        }

        partial void OnLyricsBlurAmountChanged(int value)
        {
            _settingsService.LyricsBlurAmount = value;
        }

        partial void OnLyricsCustomFontColorChanged(Color value)
        {
            _settingsService.LyricsCustomFontColor = value;
        }

        partial void OnLyricsFontColorTypeChanged(LyricsFontColorType value)
        {
            _settingsService.LyricsFontColorType = value;
        }

        partial void OnLyricsFontSizeChanged(int value)
        {
            _settingsService.LyricsFontSize = value;
        }

        partial void OnLyricsFontWeightChanged(LyricsFontWeight value)
        {
            _settingsService.LyricsFontWeight = value;
        }

        partial void OnLyricsGlowEffectScopeChanged(LineRenderingType value)
        {
            _settingsService.LyricsGlowEffectScope = value;
        }

        partial void OnLyricsLineSpacingFactorChanged(float value)
        {
            _settingsService.LyricsLineSpacingFactor = value;
        }

        partial void OnLyricsVerticalEdgeOpacityChanged(int value)
        {
            _settingsService.LyricsVerticalEdgeOpacity = value;
        }
    }
}
