using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.Lyrics;
using BetterLyrics.WinUI3.Services.GSMTCService;
using BetterLyrics.WinUI3.Services.SMTCService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class NowPlayingBarViewModel : BaseViewModel
    {
        private readonly IGSMTCService _gsmtcService;
        private readonly ISMTCService _smtcService;

        [ObservableProperty]
        public partial int Volume { get; set; }

        [ObservableProperty]
        public partial float TimelineSliderThumbOpacity { get; set; } = 0f;

        [ObservableProperty]
        public partial LyricsLine? TimelineSliderThumbLyricsLine { get; set; }

        [ObservableProperty]
        public partial double TimelineSliderThumbSeconds { get; set; } = 0;

        [ObservableProperty]
        public partial double BottomCommandGridOpacity { get; set; } = 1;

        [ObservableProperty]
        public partial double BottomCommandFlyoutTriggerOpacity { get; set; }

        public NowPlayingBarViewModel(IGSMTCService mediaSessionsService, ISMTCService smtcService)
        {
            _gsmtcService = mediaSessionsService;
            _smtcService = smtcService;

            UpdateVolume();
        }

        public void UpdateVolume()
        {
            Volume = AudioMixerHook.GetApplicationVolume(_gsmtcService.CurrentMediaSourceProviderInfo?.Provider);
        }

        partial void OnTimelineSliderThumbSecondsChanged(double value)
        {
            TimelineSliderThumbLyricsLine = _gsmtcService.CurrentLyricsData?.GetLyricsLine(value);
        }


        [RelayCommand]
        private async Task PlaySongAsync()
        {
            await _gsmtcService.PlayAsync();
        }

        [RelayCommand]
        private async Task PauseSongAsync()
        {
            await _gsmtcService.PauseAsync();
        }

        [RelayCommand]
        private async Task PreviousSongAsync()
        {
            await _gsmtcService.PreviousAsync();
        }

        [RelayCommand]
        private async Task NextSongAsync()
        {
            await _gsmtcService.NextAsync();
        }

        [RelayCommand]
        private async Task StopTrackAsync()
        {
            await _smtcService.PlayTrackAtAsync(-1);
        }

        [RelayCommand]
        private static void OpenSettingsWindow()
        {
            WindowHook.OpenOrShowWindow<SettingsWindow>();
        }

        [RelayCommand]
        private static void OpenLyricsSearchWindow()
        {
            WindowHook.OpenOrShowWindow<LyricsSearchWindow>();
        }

    }
}
