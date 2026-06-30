// 2025/6/23 by Zhe Fang

using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using BetterLyrics.WinUI3.Services.GSMTCService;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class NowPlayingPageViewModel : BaseViewModel
    {
        public IGSMTCService MediaSessionsService { get; }

        [ObservableProperty] public partial LyricsCardConfig LyricsCardConfig { get; set; }

        public NowPlayingPageViewModel(IGSMTCService mediaSessionsService)
        {
            MediaSessionsService = mediaSessionsService;
        }

    }
}
