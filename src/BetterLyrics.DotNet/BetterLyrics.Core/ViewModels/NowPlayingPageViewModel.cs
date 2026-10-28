// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Interfaces.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// Transient ViewModel
/// </summary>
public partial class NowPlayingPageViewModel(IGsmtcService mediaSessionsService) : ObservableObject, ITransientViewModel
{
    [ObservableProperty] public partial IGsmtcService MediaSessionsService { get; set; } = mediaSessionsService;
}