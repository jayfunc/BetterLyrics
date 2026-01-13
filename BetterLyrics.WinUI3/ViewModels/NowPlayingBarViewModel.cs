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
        public IGSMTCService GSMTCService { get; private set; }

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
            GSMTCService = mediaSessionsService;
            _smtcService = smtcService;

            Volume = SystemVolumeHook.MasterVolume;
            SystemVolumeHook.VolumeNotification += SystemVolumeHelper_VolumeNotification;
        }

        private void SystemVolumeHelper_VolumeNotification(object? sender, int e)
        {
            Volume = e;
        }

        partial void OnTimelineSliderThumbSecondsChanged(double value)
        {
            TimelineSliderThumbLyricsLine = GSMTCService.CurrentLyricsData?.GetLyricsLine(value);
        }


        [RelayCommand]
        private async Task PlaySongAsync()
        {
            await GSMTCService.PlayAsync();
        }

        [RelayCommand]
        private async Task PauseSongAsync()
        {
            await GSMTCService.PauseAsync();
        }

        [RelayCommand]
        private async Task PreviousSongAsync()
        {
            await GSMTCService.PreviousAsync();
        }

        [RelayCommand]
        private async Task NextSongAsync()
        {
            await GSMTCService.NextAsync();
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
