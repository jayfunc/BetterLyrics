// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsPageViewModel : BaseViewModel, IRecipient<PropertyChangedMessage<bool>>
    {
        private readonly IPlaybackService _playbackService;

        private LyricsDisplayType? _preferredDisplayTypeBeforeSwitchToNonStandardMode;

        public LyricsPageViewModel(ISettingsService settingsService, IPlaybackService playbackService) : base(settingsService)
        {
            IsFirstRun = _settingsService.IsFirstRun;
            IsTranslationEnabled = _settingsService.IsTranslationEnabled;
            PreferredDisplayType = _settingsService.PreferredDisplayType;
            ResetPositionOffsetOnSongChanged = _settingsService.ResetPositionOffsetOnSongChanged;
            PositionOffset = _settingsService.PositionOffset;
            IsImmersiveMode = _settingsService.IsImmersiveMode;
            OnIsImmersiveModeChanged(IsImmersiveMode);

            //Volume = SystemVolumeHelper.GetMasterVolume();
            //SystemVolumeHelper.VolumeChanged += SystemVolumeHelper_VolumeChanged;

            _playbackService = playbackService;
            _playbackService.SongInfoChanged += PlaybackService_SongInfoChanged;
            _playbackService.IsPlayingChanged += PlaybackService_IsPlayingChanged;
        }

        //private void SystemVolumeHelper_VolumeChanged(int volume)
        //{
        //    Volume = volume;
        //}

        private void PlaybackService_IsPlayingChanged(object? sender, Events.IsPlayingChangedEventArgs e)
        {
            IsSongPlaying = e.IsPlaying;
        }

        private void PlaybackService_SongInfoChanged(object? sender, Events.SongInfoChangedEventArgs e)
        {
            SongInfo = e.SongInfo;
            if (ResetPositionOffsetOnSongChanged)
            {
                PositionOffset = 0;
            }
            TrySwitchToPreferredDisplayType(e.SongInfo);
        }

        //[ObservableProperty]
        //public partial int Volume { get; set; }

        [ObservableProperty]
        public partial bool IsImmersiveMode { get; set; }

        [ObservableProperty]
        public partial float BottomCommandGridOpacity { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsDisplayType DisplayType { get; set; } = LyricsDisplayType.PlaceholderOnly;

        [ObservableProperty]
        public partial bool IsFirstRun { get; set; }

        [ObservableProperty]
        public partial bool IsWelcomeTeachingTipOpen { get; set; }

        [ObservableProperty]
        public partial LyricsDisplayType PreferredDisplayType { get; set; }

        [ObservableProperty]
        public partial SongInfo? SongInfo { get; set; } = null;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial int PositionOffset { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsTranslationEnabled { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool ResetPositionOffsetOnSongChanged { get; set; }

        [ObservableProperty]
        public partial bool IsSongPlaying { get; set; }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is LyricsWindowViewModel)
            {
                if (message.PropertyName == nameof(LyricsWindowViewModel.IsDockMode))
                {
                    SetNonStandardModePreferredDisplayType(message.NewValue);
                    TrySwitchToPreferredDisplayType(SongInfo);
                }
                else if (message.PropertyName == nameof(LyricsWindowViewModel.IsDesktopMode))
                {
                    SetNonStandardModePreferredDisplayType(message.NewValue);
                    TrySwitchToPreferredDisplayType(SongInfo);
                }
                else if (message.PropertyName == nameof(LyricsWindowViewModel.IsImmersiveMode))
                {
                    IsImmersiveMode = message.NewValue;
                }
            }
        }

        [RelayCommand]
        private static void OpenSettingsWindow()
        {
            WindowHelper.OpenOrShowWindow<SettingsWindow>();
        }

        [RelayCommand]
        private async Task PlaySongAsync()
        {
            await _playbackService.PlayAsync();
        }

        [RelayCommand]
        private async Task PauseSongAsync()
        {
            await _playbackService.PauseAsync();
        }

        [RelayCommand]
        private async Task PreviousSongAsync()
        {
            await _playbackService.PreviousAsync();
        }

        [RelayCommand]
        private async Task NextSongAsync()
        {
            await _playbackService.NextAsync();
        }

        private void SetNonStandardModePreferredDisplayType(bool isEnabled)
        {
            if (isEnabled)
            {
                _preferredDisplayTypeBeforeSwitchToNonStandardMode = PreferredDisplayType;
                PreferredDisplayType = LyricsDisplayType.LyricsOnly;
            }
            else
            {
                PreferredDisplayType = _preferredDisplayTypeBeforeSwitchToNonStandardMode ?? LyricsDisplayType.SplitView;
            }
        }

        private void TrySwitchToPreferredDisplayType(SongInfo? songInfo)
        {
            LyricsDisplayType displayType;

            if (songInfo == null)
            {
                displayType = LyricsDisplayType.PlaceholderOnly;
            }
            else if (PreferredDisplayType is LyricsDisplayType preferredDisplayType)
            {
                displayType = preferredDisplayType;
            }
            else
            {
                displayType = LyricsDisplayType.SplitView;
            }

            DisplayType = displayType;

        }

        partial void OnIsFirstRunChanged(bool value)
        {
            IsWelcomeTeachingTipOpen = value;
            _settingsService.IsFirstRun = false;
        }

        partial void OnIsTranslationEnabledChanged(bool value)
        {
            _settingsService.IsTranslationEnabled = value;
        }

        partial void OnPreferredDisplayTypeChanged(LyricsDisplayType value)
        {
            _settingsService.PreferredDisplayType = value;
        }

        partial void OnPositionOffsetChanged(int value)
        {
            _settingsService.PositionOffset = value;
        }

        partial void OnIsImmersiveModeChanged(bool value)
        {
            if (value)
            {
                BottomCommandGridOpacity = 0f;
            }
            else
            {
                BottomCommandGridOpacity = .5f;
            }
        }

        //partial void OnVolumeChanged(int value)
        //{
        //    SystemVolumeHelper.SetMasterVolume(value);
        //}
    }
}
