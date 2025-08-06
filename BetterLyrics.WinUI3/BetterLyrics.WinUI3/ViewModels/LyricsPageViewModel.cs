// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsPageViewModel : BaseViewModel,
        IRecipient<PropertyChangedMessage<bool>>,
        IRecipient<PropertyChangedMessage<int>>,
        IRecipient<PropertyChangedMessage<string>>,
        IRecipient<PropertyChangedMessage<TimeSpan>>,
        IRecipient<PropertyChangedMessage<LyricsSearchProvider?>>,
        IRecipient<PropertyChangedMessage<TranslationSearchProvider?>>
    {
        private readonly IMediaSessionsService _mediaSessionsService;
        private readonly ThrottleHelper _timelineThrottle = new(TimeSpan.FromSeconds(1));

        private bool _isDockMode = false;
        private bool _isDesktopMode = false;

        private int _lyricsStandardFontSize = 8;
        private int _lyricsDockFontSize = 8;
        private int _lyricsDesktopFontSize = 8;

        public LyricsPageViewModel(ISettingsService settingsService, IMediaSessionsService mediaSessionsService) : base(settingsService)
        {
            IsFirstRun = _settingsService.IsFirstRun;
            IsTranslationEnabled = _settingsService.IsTranslationEnabled;
            DisplayType = _settingsService.DisplayType;
            PositionOffset = _settingsService.PositionOffset;
            IsImmersiveMode = _settingsService.IsImmersiveMode;
            ShowTranslationOnly = _settingsService.ShowTranslationOnly;

            UpdateHintMessageFontSize();

            LyricsFontFamily = _settingsService.LyricsFontFamily;

            OnIsImmersiveModeChanged(IsImmersiveMode);

            //Volume = SystemVolumeHelper.GetMasterVolume();
            //SystemVolumeHelper.VolumeChanged += SystemVolumeHelper_VolumeChanged;

            _mediaSessionsService = mediaSessionsService;
            _mediaSessionsService.SongInfoChanged += PlaybackService_SongInfoChanged;
            _mediaSessionsService.IsPlayingChanged += PlaybackService_IsPlayingChanged;
            _mediaSessionsService.TimelineChanged += PlaybackService_TimelineChanged;

            IsSongPlaying = _mediaSessionsService.IsPlaying;
        }

        private void PlaybackService_TimelineChanged(object? sender, Events.TimelineChangedEventArgs e)
        {
            SongDurationSeconds = (int)e.End.TotalSeconds;
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
        public partial int HintMessageFontSize { get; set; }

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
        public partial bool IsSongPlaying { get; set; }

        [ObservableProperty]
        public partial LyricsSearchProvider? LyricsSearchProvider { get; set; } = null;

        [ObservableProperty]
        public partial TranslationSearchProvider? TranslationSearchProvider { get; set; } = null;

        private void UpdateHintMessageFontSize()
        {
            if (_isDockMode)
            {
                HintMessageFontSize = _settingsService.LyricsDockFontSize;
            }
            else if (_isDesktopMode)
            {
                HintMessageFontSize = _settingsService.LyricsDesktopFontSize;
            }
            else
            {
                HintMessageFontSize = _settingsService.LyricsStandardFontSize;
            }
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is LyricsWindowViewModel)
            {
                if (message.PropertyName == nameof(LyricsWindowViewModel.IsDockMode))
                {
                    _isDockMode = message.NewValue;
                    if (message.NewValue)
                    {
                        DisplayType = LyricsDisplayType.LyricsOnly;
                    }
                    else
                    {
                        DisplayType = _settingsService.DisplayType;
                    }
                    UpdateHintMessageFontSize();
                }
                else if (message.PropertyName == nameof(LyricsWindowViewModel.IsDesktopMode))
                {
                    _isDesktopMode = message.NewValue;
                    if (message.NewValue)
                    {
                        DisplayType = LyricsDisplayType.LyricsOnly;
                    }
                    else
                    {
                        DisplayType = _settingsService.DisplayType;
                    }
                    UpdateHintMessageFontSize();
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
            await _mediaSessionsService.PlayAsync();
        }

        [RelayCommand]
        private async Task PauseSongAsync()
        {
            await _mediaSessionsService.PauseAsync();
        }

        [RelayCommand]
        private async Task PreviousSongAsync()
        {
            await _mediaSessionsService.PreviousAsync();
        }

        [RelayCommand]
        private async Task NextSongAsync()
        {
            await _mediaSessionsService.NextAsync();
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
            if (message.Sender is SettingsPageViewModel.SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsStandardFontSize))
                {
                    UpdateHintMessageFontSize();
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsDockFontSize))
                {
                    UpdateHintMessageFontSize();
                }
                else if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsDesktopFontSize))
                {
                    UpdateHintMessageFontSize();
                }
            }
        }

        public void Receive(PropertyChangedMessage<string> message)
        {
            if (message.Sender is SettingsPageViewModel.SettingsPageViewModel)
            {
                if (message.PropertyName == nameof(SettingsPageViewModel.SettingsPageViewModel.LyricsFontFamily))
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
            if (message.Sender is LyricsRendererViewModel.LyricsRendererViewModel)
            {
                if (message.PropertyName == nameof(LyricsRendererViewModel.LyricsRendererViewModel.TotalTime))
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

        public void Receive(PropertyChangedMessage<LyricsSearchProvider?> message)
        {
            if (message.Sender is LyricsRendererViewModel.LyricsRendererViewModel)
            {
                if (message.PropertyName == nameof(LyricsRendererViewModel.LyricsRendererViewModel.LyricsSearchProvider))
                {
                    LyricsSearchProvider = message.NewValue;
                }
            }
        }

        public void Receive(PropertyChangedMessage<TranslationSearchProvider?> message)
        {
            if (message.Sender is LyricsRendererViewModel.LyricsRendererViewModel)
            {
                if (message.PropertyName == nameof(LyricsRendererViewModel.LyricsRendererViewModel.TranslationSearchProvider))
                {
                    TranslationSearchProvider = message.NewValue;
                }
            }
        }
    }
}
