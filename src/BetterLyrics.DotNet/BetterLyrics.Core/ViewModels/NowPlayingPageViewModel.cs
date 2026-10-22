// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Interfaces.ViewModels;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// Transient ViewModel
/// </summary>
public partial class NowPlayingPageViewModel(IGsmtcService mediaSessionsService) : ITransientViewModel
{
    public IGsmtcService MediaSessionsService { get; } = mediaSessionsService;
}