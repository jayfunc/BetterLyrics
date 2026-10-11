using System;
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
    private readonly IAudioMixerService _audioMixerService;
    private readonly ISmtcService _smtcService;

    public NowPlayingBarViewModel(IGsmtcService mediaSessionsService, ISettingsService settingsService,
        INavigationService navigationService, IAudioMixerService audioMixerService,
        ISmtcService smtcService)
    {
        _gsmtcService = mediaSessionsService;
        _settingsService = settingsService;
        _audioMixerService = audioMixerService;
        _smtcService = smtcService;

        NavigationService = navigationService;
        AppSettings = _settingsService.AppSettings;

        UpdateVolume();
    }

    public INavigationService NavigationService { get; }

    [ObservableProperty] public partial AppSettings AppSettings { get; set; }

    [ObservableProperty] public partial int Volume { get; set; } = -1;
    [ObservableProperty] public partial int SystemVolume { get; set; } = -1;

    [ObservableProperty] public partial LyricsLine? TimelineSliderThumbLyricsLine { get; set; }

    [ObservableProperty] public partial double TimelineSliderThumbSeconds { get; set; } = 0;

    public void UpdateVolume()
    {
        var provider = _gsmtcService.CurrentMediaSourceProviderInfo?.Provider;
        var isBetterLyrics = _gsmtcService.CurrentMediaSourceProviderInfo?.IsBetterLyrics == true ||
            (_gsmtcService.CurrentMediaSourceProviderInfo == null && _smtcService.PlayingTrack != null);

        var appVolume = _audioMixerService.GetApplicationVolume(provider);

        if (isBetterLyrics)
        {
            var savedVolume = _settingsService.AppSettings.MusicGallerySettings.Volume;
            if (appVolume == -1)
            {
                appVolume = savedVolume;
            }
            else if (!_smtcService.IsVolumeRestored && appVolume != savedVolume)
            {
                _audioMixerService.SetApplicationVolume(Environment.ProcessId, savedVolume);
                _smtcService.IsVolumeRestored = true;
                appVolume = savedVolume;
            }
        }

        Volume = appVolume;
        SystemVolume = _audioMixerService.GetSystemVolume();
    }

    public void ApplyVolume()
    {
        var provider = _gsmtcService.CurrentMediaSourceProviderInfo?.Provider;
        var isBetterLyrics = _gsmtcService.CurrentMediaSourceProviderInfo?.IsBetterLyrics == true ||
            (_gsmtcService.CurrentMediaSourceProviderInfo == null && _smtcService.PlayingTrack != null);

        if (isBetterLyrics)
        {
            _audioMixerService.SetApplicationVolume(Environment.ProcessId, Volume);
            _settingsService.AppSettings.MusicGallerySettings.Volume = Volume;
            _smtcService.IsVolumeRestored = true;
        }
        else
        {
            _audioMixerService.SetApplicationVolume(provider, Volume);
        }
    }
    
    public void ApplySystemVolume()
    {
        _audioMixerService.SetSystemVolume(SystemVolume);
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