using System;
using System.Diagnostics;
using System.Threading.Tasks;
using BetterLyrics.WinUI3;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Playback;
using BetterLyrics.WinUI3.Services.Settings;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;

namespace BetterInAppLyrics.WinUI3.ViewModels
{
    public partial class InAppLyricsSettingsControlViewModel
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

        private readonly ISettingsService _settingsService;

        public InAppLyricsSettingsControlViewModel(
            ISettingsService settingsService,
            IPlaybackService playbackService
        )
            : base(settingsService, playbackService)
        {
            IsActive = true;
            _settingsService = settingsService;

            LyricsAlignmentType = _settingsService.InAppLyricsAlignmentType;
            LyricsBlurAmount = _settingsService.InAppLyricsBlurAmount;
            LyricsVerticalEdgeOpacity = _settingsService.InAppLyricsVerticalEdgeOpacity;
            LyricsLineSpacingFactor = _settingsService.InAppLyricsLineSpacingFactor;
            LyricsFontSize = _settingsService.InAppLyricsFontSize;
            IsLyricsGlowEffectEnabled = _settingsService.IsInAppLyricsGlowEffectEnabled;
            IsLyricsDynamicGlowEffectEnabled =
                _settingsService.IsInAppLyricsDynamicGlowEffectEnabled;
            LyricsFontColorType = _settingsService.InAppLyricsFontColorType;
            LyricsFontSelectedAccentColorIndex =
                _settingsService.InAppLyricsFontSelectedAccentColorIndex;
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

        partial void OnLyricsFontSelectedAccentColorIndexChanged(int value)
        {
            _settingsService.InAppLyricsFontSelectedAccentColorIndex = value;
        }
    }
}
