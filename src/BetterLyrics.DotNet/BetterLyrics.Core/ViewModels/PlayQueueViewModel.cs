using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Interfaces.ViewModels;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// Transient ViewModel
/// </summary>
public partial class PlayQueueViewModel : ObservableObject, ITransientViewModel
{
    private readonly ISettingsService _settingsService;

    public ISmtcService SMTCService { get; set; }

    [ObservableProperty] public partial AppSettings AppSettings { get; set; }

    public PlayQueueViewModel(ISmtcService smtcService, ISettingsService settingsService)
    {
        _settingsService = settingsService;
        SMTCService = smtcService;
        AppSettings = _settingsService.AppSettings;
    }

    [RelayCommand]
    private void EmptyPlayingQueue()
    {
        SMTCService.TrackPlayingQueue.Clear(); // Reset
        SMTCService.PlaybackState.PlayQueueIndex = -1;
        SMTCService.PlayTrackAt(-1); // Stop playing
    }

    [RelayCommand]
    private void RemoveFromPlayingQueue(PlayQueueItem item)
    {
        SMTCService.TrackPlayingQueue.Remove(item);
        if (SMTCService.TrackPlayingQueue.Count == 0)
        {
            SMTCService.PlaybackState.PlayQueueIndex = -1;
            SMTCService.PlayTrackAt(-1); // Stop playing
        }
    }

    [RelayCommand]
    private void PlayTrack(PlayQueueItem item)
    {
        SMTCService.PlayTrack(item);
    }
}