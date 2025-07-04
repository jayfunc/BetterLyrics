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
using System.Diagnostics;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsPageViewModel : BaseViewModel, IRecipient<PropertyChangedMessage<int>>, IRecipient<PropertyChangedMessage<bool>>
    {
        private readonly IPlaybackService _playbackService;

        private LyricsDisplayType? _preferredDisplayTypeBeforeSwitchToNonStandardMode;

        public LyricsPageViewModel(ISettingsService settingsService, IPlaybackService playbackService) : base(settingsService)
        {
            LyricsFontSize = _settingsService.LyricsFontSize;

            _playbackService = playbackService;
            _playbackService.SongInfoChanged += PlaybackService_SongInfoChanged;


            IsFirstRun = _settingsService.IsFirstRun;
        }

        private void PlaybackService_SongInfoChanged(object? sender, Events.SongInfoChangedEventArgs e)
        {
            SongInfo = e.SongInfo;
            TrySwitchToPreferredDisplayType(e.SongInfo);
        }

        [ObservableProperty]
        [NotifyPropertyChangedRecipients]
        public partial LyricsDisplayType DisplayType { get; set; } = LyricsDisplayType.PlaceholderOnly;

        [ObservableProperty]
        public partial bool IsFirstRun { get; set; }

        [ObservableProperty]
        public partial bool IsNotDockMode { get; set; } = true;

        [ObservableProperty]
        public partial bool IsWelcomeTeachingTipOpen { get; set; }

        [ObservableProperty]
        public partial int LyricsFontSize { get; set; }

        [ObservableProperty]
        public partial LyricsDisplayType? PreferredDisplayType { get; set; } = LyricsDisplayType.SplitView;

        [ObservableProperty]
        public partial SongInfo? SongInfo { get; set; } = null;

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is LyricsWindowViewModel)
            {
                if (message.PropertyName == nameof(LyricsWindowViewModel.IsDockMode))
                {
                    IsNotDockMode = !message.NewValue;
                    SetNonStandardModePreferredDisplayType(message.NewValue);
                    TrySwitchToPreferredDisplayType(SongInfo);
                }
                else if (message.PropertyName == nameof(LyricsWindowViewModel.IsDesktopMode))
                {
                    SetNonStandardModePreferredDisplayType(message.NewValue);
                    TrySwitchToPreferredDisplayType(SongInfo);
                }
            }
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

        [RelayCommand]
        private void OpenSettingsWindow()
        {
            WindowHelper.OpenOrShowWindow<SettingsWindow>();
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
                PreferredDisplayType = _preferredDisplayTypeBeforeSwitchToNonStandardMode;
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
    }
}
