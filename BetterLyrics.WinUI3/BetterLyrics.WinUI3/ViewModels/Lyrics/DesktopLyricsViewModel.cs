using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Playback;
using BetterLyrics.WinUI3.Services.Settings;
using BetterLyrics.WinUI3.ViewModels.Lyrics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Documents;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class DesktopLyricsViewModel : BaseLyricsViewModel, ILyricsViewModel
    {
        private LyricsAlignmentType? _lyricsAlignmentType;
        public LyricsAlignmentType LyricsAlignmentType
        {
            get
            {
                _lyricsAlignmentType ??= (LyricsAlignmentType)Get(
                    SettingsKeys.DesktopLyricsAlignmentType,
                    SettingsDefaultValues.DesktopLyricsAlignmentType
                );
                return _lyricsAlignmentType ?? 0;
            }
            set
            {
                Set(SettingsKeys.DesktopLyricsAlignmentType, (int)value);
                _lyricsAlignmentType = value;
            }
        }
        private int? _lyricsBlurAmount;
        public int LyricsBlurAmount
        {
            get
            {
                _lyricsBlurAmount ??= Get(
                    SettingsKeys.DesktopLyricsBlurAmount,
                    SettingsDefaultValues.DesktopLyricsBlurAmount
                );
                return _lyricsBlurAmount ?? 0;
            }
            set
            {
                Set(SettingsKeys.DesktopLyricsBlurAmount, value);
                _lyricsBlurAmount = value;
            }
        }

        private int? _lyricsVerticalEdgeOpacity;
        public int LyricsVerticalEdgeOpacity
        {
            get
            {
                _lyricsVerticalEdgeOpacity ??= Get(
                    SettingsKeys.DesktopLyricsVerticalEdgeOpacity,
                    SettingsDefaultValues.DesktopLyricsVerticalEdgeOpacity
                );
                return _lyricsVerticalEdgeOpacity ?? 0;
            }
            set
            {
                Set(SettingsKeys.DesktopLyricsVerticalEdgeOpacity, value);
                _lyricsVerticalEdgeOpacity = value;
            }
        }

        private float? _lyricsLineSpacingFactor;
        public float LyricsLineSpacingFactor
        {
            get
            {
                _lyricsLineSpacingFactor ??= Get(
                    SettingsKeys.DesktopLyricsLineSpacingFactor,
                    SettingsDefaultValues.DesktopLyricsLineSpacingFactor
                );
                return _lyricsLineSpacingFactor ?? 0;
            }
            set
            {
                Set(SettingsKeys.DesktopLyricsLineSpacingFactor, value);
                _lyricsLineSpacingFactor = value;
            }
        }

        private int? _lyricsFontSize;
        public int LyricsFontSize
        {
            get
            {
                _lyricsFontSize ??= Get(
                    SettingsKeys.DesktopLyricsFontSize,
                    SettingsDefaultValues.DesktopLyricsFontSize
                );
                return _lyricsFontSize ?? 0;
            }
            set
            {
                Set(SettingsKeys.DesktopLyricsFontSize, value);
                _lyricsFontSize = value;
            }
        }

        private bool? _isLyricsGlowEffectEnabled;
        public bool IsLyricsGlowEffectEnabled
        {
            get
            {
                _isLyricsGlowEffectEnabled ??= Get(
                    SettingsKeys.IsDesktopLyricsGlowEffectEnabled,
                    SettingsDefaultValues.IsDesktopLyricsGlowEffectEnabled
                );
                return _isLyricsGlowEffectEnabled ?? false;
            }
            set
            {
                Set(SettingsKeys.IsDesktopLyricsGlowEffectEnabled, value);
                _isLyricsGlowEffectEnabled = value;
            }
        }

        private bool? _isLyricsDynamicGlowEffectEnabled;
        public bool IsLyricsDynamicGlowEffectEnabled
        {
            get
            {
                _isLyricsDynamicGlowEffectEnabled ??= Get(
                    SettingsKeys.IsDesktopLyricsDynamicGlowEffectEnabled,
                    SettingsDefaultValues.IsDesktopLyricsDynamicGlowEffectEnabled
                );
                return _isLyricsDynamicGlowEffectEnabled ?? false;
            }
            set
            {
                Set(SettingsKeys.IsDesktopLyricsDynamicGlowEffectEnabled, value);
                _isLyricsDynamicGlowEffectEnabled = value;
            }
        }

        private LyricsFontColorType? _lyricsFontColorType;
        public LyricsFontColorType LyricsFontColorType
        {
            get
            {
                _lyricsFontColorType ??= (LyricsFontColorType)Get(
                    SettingsKeys.DesktopLyricsFontColorType,
                    SettingsDefaultValues.DesktopLyricsFontColorType
                );
                return _lyricsFontColorType ?? 0;
            }
            set
            {
                Set(SettingsKeys.DesktopLyricsFontColorType, (int)value);
                _lyricsFontColorType = value;
                WeakReferenceMessenger.Default.Send(new LyricsFontColorChangedMessage());
            }
        }

        private int? _lyricsFontSelectedAccentColorIndex;
        public int LyricsFontSelectedAccentColorIndex
        {
            get
            {
                _lyricsFontSelectedAccentColorIndex ??= Get(
                    SettingsKeys.DesktopLyricsFontSelectedAccentColorIndex,
                    SettingsDefaultValues.DesktopLyricsFontSelectedAccentColorIndex
                );
                return _lyricsFontSelectedAccentColorIndex ?? 0;
            }
            set
            {
                if (value >= 0)
                {
                    Set(SettingsKeys.DesktopLyricsFontSelectedAccentColorIndex, value);
                    _lyricsFontSelectedAccentColorIndex = value;
                    WeakReferenceMessenger.Default.Send(new LyricsFontColorChangedMessage());
                }
            }
        }

        [ObservableProperty]
        private bool _isSettingsPopupOpened = false;

        [RelayCommand]
        private void ToggleSettingsPopup()
        {
            IsSettingsPopupOpened = !IsSettingsPopupOpened;
        }

        private readonly IPlaybackService _playbackService;

        public DesktopLyricsViewModel(
            ISettingsService settingsService,
            IPlaybackService playbackService
        )
            : base(settingsService)
        {
            _playbackService = playbackService;

            _playbackService.ReSendingMessages();
        }
    }
}
