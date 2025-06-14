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
using BetterLyrics.WinUI3.ViewModels.Lyrics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml.Media.Imaging;

namespace BetterInAppLyrics.WinUI3.ViewModels
{
    public partial class InAppLyricsViewModel : BaseLyricsViewModel, ILyricsViewModel
    {
        private LyricsAlignmentType? _lyricsAlignmentType;
        public LyricsAlignmentType LyricsAlignmentType
        {
            get
            {
                _lyricsAlignmentType ??= (LyricsAlignmentType)Get(
                    SettingsKeys.InAppLyricsAlignmentType,
                    SettingsDefaultValues.InAppLyricsAlignmentType
                );
                return _lyricsAlignmentType ?? 0;
            }
            set
            {
                _lyricsAlignmentType = value;
                Set(SettingsKeys.InAppLyricsAlignmentType, (int)value);
            }
        }
        private int? _lyricsBlurAmount;
        public int LyricsBlurAmount
        {
            get
            {
                _lyricsBlurAmount ??= Get(
                    SettingsKeys.InAppLyricsBlurAmount,
                    SettingsDefaultValues.InAppLyricsBlurAmount
                );
                return _lyricsBlurAmount ?? 0;
            }
            set
            {
                _lyricsBlurAmount = value;
                Set(SettingsKeys.InAppLyricsBlurAmount, value);
            }
        }

        private int? _lyricsVerticalEdgeOpacity;
        public int LyricsVerticalEdgeOpacity
        {
            get
            {
                _lyricsVerticalEdgeOpacity ??= Get(
                    SettingsKeys.InAppLyricsVerticalEdgeOpacity,
                    SettingsDefaultValues.InAppLyricsVerticalEdgeOpacity
                );
                return _lyricsVerticalEdgeOpacity ?? 0;
            }
            set
            {
                _lyricsVerticalEdgeOpacity = value;
                Set(SettingsKeys.InAppLyricsVerticalEdgeOpacity, value);
            }
        }

        private float? _lyricsLineSpacingFactor;
        public float LyricsLineSpacingFactor
        {
            get
            {
                _lyricsLineSpacingFactor ??= Get(
                    SettingsKeys.InAppLyricsLineSpacingFactor,
                    SettingsDefaultValues.InAppLyricsLineSpacingFactor
                );
                return _lyricsLineSpacingFactor ?? 0;
            }
            set
            {
                _lyricsLineSpacingFactor = value;
                Set(SettingsKeys.InAppLyricsLineSpacingFactor, value);
            }
        }

        private int? _lyricsFontSize;
        public int LyricsFontSize
        {
            get
            {
                _lyricsFontSize ??= Get(
                    SettingsKeys.InAppLyricsFontSize,
                    SettingsDefaultValues.InAppLyricsFontSize
                );
                return _lyricsFontSize ?? 0;
            }
            set
            {
                _lyricsFontSize = value;
                Set(SettingsKeys.InAppLyricsFontSize, value);
            }
        }

        private bool? _isLyricsGlowEffectEnabled;
        public bool IsLyricsGlowEffectEnabled
        {
            get
            {
                _isLyricsGlowEffectEnabled ??= Get(
                    SettingsKeys.IsInAppLyricsGlowEffectEnabled,
                    SettingsDefaultValues.IsInAppLyricsGlowEffectEnabled
                );
                return _isLyricsGlowEffectEnabled ?? false;
            }
            set
            {
                _isLyricsGlowEffectEnabled = value;
                Set(SettingsKeys.IsInAppLyricsGlowEffectEnabled, value);
            }
        }

        private bool? _isLyricsDynamicGlowEffectEnabled;
        public bool IsLyricsDynamicGlowEffectEnabled
        {
            get
            {
                _isLyricsDynamicGlowEffectEnabled ??= Get(
                    SettingsKeys.IsInAppLyricsDynamicGlowEffectEnabled,
                    SettingsDefaultValues.IsInAppLyricsDynamicGlowEffectEnabled
                );
                return _isLyricsDynamicGlowEffectEnabled ?? false;
            }
            set
            {
                _isLyricsDynamicGlowEffectEnabled = value;
                Set(SettingsKeys.IsInAppLyricsDynamicGlowEffectEnabled, value);
            }
        }

        private LyricsFontColorType? _lyricsFontColorType;
        public LyricsFontColorType LyricsFontColorType
        {
            get
            {
                _lyricsFontColorType ??= (LyricsFontColorType)Get(
                    SettingsKeys.InAppLyricsFontColorType,
                    SettingsDefaultValues.InAppLyricsFontColorType
                );
                return _lyricsFontColorType ?? 0;
            }
            set
            {
                _lyricsFontColorType = value;
                Set(SettingsKeys.InAppLyricsFontColorType, (int)value);
            }
        }

        private int? _lyricsFontSelectedAccentColorIndex;
        public int LyricsFontSelectedAccentColorIndex
        {
            get
            {
                _lyricsFontSelectedAccentColorIndex ??= Get(
                    SettingsKeys.InAppLyricsFontSelectedAccentColorIndex,
                    SettingsDefaultValues.InAppLyricsFontSelectedAccentColorIndex
                );
                return _lyricsFontSelectedAccentColorIndex ?? 0;
            }
            set
            {
                if (value >= 0)
                {
                    _lyricsFontSelectedAccentColorIndex = value;
                    Set(SettingsKeys.InAppLyricsFontSelectedAccentColorIndex, value);
                }
            }
        }

        //

        [ObservableProperty]
        private BitmapImage? _coverImage;

        [ObservableProperty]
        private SongInfo? _songInfo = null;

        private DisplayType? _preferredDisplayType = DisplayType.SplitView;

        [ObservableProperty]
        private bool _aboutToUpdateUI;

        private bool _isImmersiveMode = false;
        public bool IsImmersiveMode
        {
            get => _isImmersiveMode;
            set
            {
                _isImmersiveMode = value;
                OnPropertyChanged();
                WeakReferenceMessenger.Default.Send(new IsImmersiveModeChangedMessage(value));
                if (value)
                    WeakReferenceMessenger.Default.Send(
                        new ShowNotificatonMessage(
                            new Notification(
                                App.ResourceLoader!.GetString("MainPageEnterImmersiveModeHint"),
                                isForeverDismissable: true,
                                relatedSettingsKeyName: SettingsKeys.NeverShowEnterImmersiveModeMessage
                            )
                        )
                    );
            }
        }

        public InAppLyricsViewModel(
            ISettingsService settingsService,
            IPlaybackService playbackService
        )
            : base(settingsService) { }

        public async Task UpdateSongInfoUI(SongInfo? songInfo)
        {
            AboutToUpdateUI = true;
            await Task.Delay(AnimationHelper.StoryboardDefaultDuration);

            SongInfo = songInfo;

            await Task.Delay(1);

            CoverImage =
                (songInfo?.AlbumArt == null)
                    ? null
                    : await ImageHelper.GetBitmapImageFromBytesAsync(songInfo.AlbumArt);

            DisplayType displayType;

            if (songInfo == null)
            {
                displayType = DisplayType.PlaceholderOnly;
            }
            else if (_preferredDisplayType is DisplayType preferredDisplayType)
            {
                displayType = preferredDisplayType;
            }
            else
            {
                displayType = DisplayType.SplitView;
            }

            WeakReferenceMessenger.Default.Send(new DisplayTypeChangedMessage(displayType));

            AboutToUpdateUI = false;
        }

        public void OpenMatchedFileFolderInFileExplorer(string path)
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{path}\"",
                    UseShellExecute = true,
                }
            );
        }

        [RelayCommand]
        private void OnDisplayTypeChanged(object value)
        {
            int index = Convert.ToInt32(value);
            _preferredDisplayType = (DisplayType)index;
            WeakReferenceMessenger.Default.Send(new DisplayTypeChangedMessage((DisplayType)index));
        }
    }
}
