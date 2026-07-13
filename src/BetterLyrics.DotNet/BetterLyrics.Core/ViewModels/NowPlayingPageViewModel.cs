// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.ViewModels;

public partial class NowPlayingPageViewModel : BaseViewModel
{
    public NowPlayingPageViewModel(IGsmtcService mediaSessionsService)
    {
        MediaSessionsService = mediaSessionsService;
    }

    public IGsmtcService MediaSessionsService { get; }

    [ObservableProperty] public partial LyricsCardConfig LyricsCardConfig { get; set; }
}