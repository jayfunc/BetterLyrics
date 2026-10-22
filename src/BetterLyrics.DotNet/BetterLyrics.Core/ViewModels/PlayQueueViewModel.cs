using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BetterLyrics.Core.Interfaces.ViewModels;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// Transient ViewModel，生命周期跟随 PlayQueue 控件实例
/// （音乐库页面与 NowPlayingBar 浮出面板中会同时存在多个实例）。
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