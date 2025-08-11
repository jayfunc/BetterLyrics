// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
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
        IRecipient<PropertyChangedMessage<TimeSpan>>
    {
        private readonly IMediaSessionsService _mediaSessionsService;
        private readonly ISettingsService _settingsService;

        private readonly ThrottleHelper _timelineThrottle = new(TimeSpan.FromSeconds(1));

        private bool _isDockMode = false;
        private bool _isDesktopMode = false;

        public LyricsPageViewModel(ISettingsService settingsService, IMediaSessionsService mediaSessionsService)
        {
            _settingsService = settingsService;
            IsTranslationEnabled = _settingsService.AppSettings.TranslationSettings.IsTranslationEnabled;
            DisplayType = _settingsService.AppSettings.GeneralSettings.DisplayType;
            IsImmersiveMode = _settingsService.AppSettings.GeneralSettings.IsImmersiveMode;
            ShowTranslationOnly = _settingsService.AppSettings.TranslationSettings.ShowTranslationOnly;

            UpdateHintMessageFontSize();

            LyricsFontFamily = _settingsService.AppSettings.StandardLyricsStyleSettings.LyricsFontFamily;

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
        public partial double BottomCommandGridOpacity { get; set; }

        [ObservableProperty]
        public partial double BottomCommandFlyoutTriggerOpacity { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsDisplayType DisplayType { get; set; }

        [ObservableProperty]
        public partial SongInfo? SongInfo { get; set; } = null;

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool IsTranslationEnabled { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial bool ShowTranslationOnly { get; set; }

        [ObservableProperty]
        public partial bool IsSongPlaying { get; set; }

        private void UpdateHintMessageFontSize()
        {
            if (_isDockMode)
            {
                HintMessageFontSize = _settingsService.AppSettings.DockLyricsStyleSettings.LyricsFontSize;
            }
            else if (_isDesktopMode)
            {
                HintMessageFontSize = _settingsService.AppSettings.DesktopLyricsStyleSettings.LyricsFontSize;
            }
            else
            {
                HintMessageFontSize = _settingsService.AppSettings.StandardLyricsStyleSettings.LyricsFontSize;
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
                        DisplayType = _settingsService.AppSettings.GeneralSettings.DisplayType;
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
                        DisplayType = _settingsService.AppSettings.GeneralSettings.DisplayType;
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

        partial void OnIsTranslationEnabledChanged(bool value)
        {
            _settingsService.AppSettings.TranslationSettings.IsTranslationEnabled = value;
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
            _settingsService.AppSettings.TranslationSettings.ShowTranslationOnly = value;
        }

        public void Receive(PropertyChangedMessage<int> message)
        {
            if (message.Sender is LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.LyricsFontSize))
                {
                    UpdateHintMessageFontSize();
                }
            }
        }

        public void Receive(PropertyChangedMessage<string> message)
        {
            if (message.Sender is LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsStyleSettings.LyricsFontFamily))
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
    }
}
