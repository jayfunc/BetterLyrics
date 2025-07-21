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
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsPageViewModel : BaseViewModel,
        IRecipient<PropertyChangedMessage<bool>>,
        IRecipient<PropertyChangedMessage<int>>,
        IRecipient<PropertyChangedMessage<string>>,
        IRecipient<PropertyChangedMessage<TimeSpan>>
    {
        private readonly IPlaybackService _playbackService;
        private readonly ThrottleHelper _timelineThrottle = new(TimeSpan.FromSeconds(1));

        public LyricsPageViewModel(ISettingsService settingsService, IPlaybackService playbackService) : base(settingsService)
        {
            IsFirstRun = _settingsService.IsFirstRun;
            IsTranslationEnabled = _settingsService.IsTranslationEnabled;
            DisplayType = _settingsService.DisplayType;
            ResetPositionOffsetOnSongChanged = _settingsService.ResetPositionOffsetOnSongChanged;
            PositionOffset = _settingsService.PositionOffset;
            IsImmersiveMode = _settingsService.IsImmersiveMode;
            ShowTranslationOnly = _settingsService.ShowTranslationOnly;
            LyricsFontSize = _settingsService.LyricsFontSize;
            LyricsFontFamily = _settingsService.LyricsFontFamily;

            OnIsImmersiveModeChanged(IsImmersiveMode);

            //Volume = SystemVolumeHelper.GetMasterVolume();
            //SystemVolumeHelper.VolumeChanged += SystemVolumeHelper_VolumeChanged;

            _playbackService = playbackService;
            _playbackService.SongInfoChanged += PlaybackService_SongInfoChanged;
            _playbackService.IsPlayingChanged += PlaybackService_IsPlayingChanged;

            IsSongPlaying = _playbackService.IsPlaying;
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
            SongDurationSeconds = SongInfo?.Duration ?? 0;
            if (ResetPositionOffsetOnSongChanged)
            {
                PositionOffset = 0;
            }
        }

        [ObservableProperty]
        public partial double TimelinePositionSeconds { get; set; }

        [ObservableProperty]
        public partial int SongDurationSeconds { get; set; }

        [ObservableProperty]
        public partial int Volume { get; set; }

        [ObservableProperty]
        public partial string LyricsFontFamily { get; set; }

        [ObservableProperty]
        public partial int LyricsFontSize { get; set; }

        [ObservableProperty]
        public partial bool IsImmersiveMode { get; set; }

        [ObservableProperty]
        public partial float BottomCommandGridOpacity { get; set; }

        [ObservableProperty]
        public partial float BottomCommandFlyoutTriggerOpacity { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsDisplayType DisplayType { get; set; }

        [ObservableProperty]
        public partial bool IsFirstRun { get; set; }

        [ObservableProperty]
        public partial bool IsWelcomeTeachingTipOpen { get; set; }

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
        public partial bool ShowTranslationOnly { get; set; }

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
                    if (message.NewValue)
                    {
                        DisplayType = LyricsDisplayType.LyricsOnly;
                    }
                    else
                    {
                        DisplayType = _settingsService.DisplayType;
                    }
                }
                else if (message.PropertyName == nameof(LyricsWindowViewModel.IsDesktopMode))
                {
                    if (message.NewValue)
                    {
                        DisplayType = LyricsDisplayType.LyricsOnly;
                    }
                    else
                    {
                        DisplayType = _settingsService.DisplayType;
                    }
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
            WindowHelper.OpenWindow<SettingsWindow>();
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

        partial void OnIsFirstRunChanged(bool value)
        {
            IsWelcomeTeachingTipOpen = value;
            _settingsService.IsFirstRun = false;
        }

        partial void OnIsTranslationEnabledChanged(bool value)
        {
            _settingsService.IsTranslationEnabled = value;
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
                BottomCommandFlyoutTriggerOpacity = 0f;
            }
            else
            {
                BottomCommandGridOpacity = 1f;
                BottomCommandFlyoutTriggerOpacity = 1f;
            }
        }

        partial void OnShowTranslationOnlyChanged(bool value)
        {
            _settingsService.ShowTranslationOnly = value;
        }

        public void Receive(PropertyChangedMessage<int> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.LyricsFontSize))
                {
                    LyricsFontSize = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<string> message)
        {
            if (message.Sender is SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.LyricsFontFamily))
                {
                    LyricsFontFamily = message.NewValue;
                }
            }
        }

        //partial void OnVolumeChanged(int value)
        //{
        //    SystemVolumeHelper.SetMasterVolume(value);
        //}

        public void Receive(PropertyChangedMessage<TimeSpan> message)
        {
            if (message.Sender is LyricsRendererViewModel)
            {
                if (message.PropertyName == nameof(LyricsRendererViewModel.TotalTime))
                {
                    if (_timelineThrottle.CanTrigger())
                    {
                        _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
                        {
                            TimelinePositionSeconds = message.NewValue.TotalSeconds;
                        });
                    }
                }
            }
        }
    }
}
