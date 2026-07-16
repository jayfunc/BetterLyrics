using System.Threading.Tasks;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Lyrics;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BetterLyrics.Core.ViewModels;

public partial class NowPlayingBarViewModel : BaseViewModel
{
    private readonly IGsmtcService _gsmtcService;
    private readonly ISettingsService _settingsService;

    public NowPlayingBarViewModel(IGsmtcService mediaSessionsService, ISettingsService settingsService,
        INavigationService navigationService)
    {
        _gsmtcService = mediaSessionsService;
        _settingsService = settingsService;

        NavigationService = navigationService;
        AppSettings = _settingsService.AppSettings;

        UpdateVolume();
    }

    public INavigationService NavigationService { get; }

    [ObservableProperty] public partial AppSettings AppSettings { get; set; }

    [ObservableProperty] public partial int Volume { get; set; }

    [ObservableProperty] public partial LyricsLine? TimelineSliderThumbLyricsLine { get; set; }

    [ObservableProperty] public partial double TimelineSliderThumbSeconds { get; set; } = 0;

    public void UpdateVolume()
    {
        //Volume = AudioMixerHook.GetApplicationVolume(_gsmtcService.CurrentMediaSourceProviderInfo?.Provider);
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
        await _gsmtcService.StopAsync();
    }
}