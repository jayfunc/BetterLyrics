// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;

using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.Input;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using WinRT.Interop;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class NowPlayingPageViewModel : BaseViewModel
    {
        public IMediaSessionsService MediaSessionsService { get; private set; }

        public NowPlayingPageViewModel(IMediaSessionsService mediaSessionsService)
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
