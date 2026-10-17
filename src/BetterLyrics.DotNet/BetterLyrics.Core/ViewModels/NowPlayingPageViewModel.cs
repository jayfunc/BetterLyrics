// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// Transient ViewModel
/// </summary>
public partial class NowPlayingPageViewModel(IGsmtcService mediaSessionsService)
{
    public IGsmtcService MediaSessionsService { get; } = mediaSessionsService;

    ~NowPlayingPageViewModel()
    {
        MemoryLeakDetector.Track(this);
        MemoryLeakDetector.ScheduleCheck(4000);
    }
}