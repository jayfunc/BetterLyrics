// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Services.GSMTCService;
using BetterLyrics.WinUI3.Services.NavigationService;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class NowPlayingPageViewModel : BaseViewModel
    {
        public IGSMTCService MediaSessionsService { get; }

        public NowPlayingPageViewModel(IGSMTCService mediaSessionsService)
        {
            MediaSessionsService = mediaSessionsService;
        }

    }
}
