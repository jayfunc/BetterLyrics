// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Hooks;

using BetterLyrics.WinUI3.Services.GSMTCService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.Input;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class NowPlayingPageViewModel : BaseViewModel
    {
        public IGSMTCService MediaSessionsService { get; private set; }

        public NowPlayingPageViewModel(IGSMTCService mediaSessionsService)
        {
            MediaSessionsService = mediaSessionsService;
        }

        [RelayCommand]
        private static void OpenSettingsWindow()
        {
            WindowHook.OpenOrShowWindow<SettingsWindow>();
        }

    }
}
