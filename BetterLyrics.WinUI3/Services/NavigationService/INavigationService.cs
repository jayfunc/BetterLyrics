using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;

namespace BetterLyrics.WinUI3.Services.NavigationService
{
    public interface INavigationService
    {
        IRelayCommand OpenSettingsWindowCommand { get; }
        IRelayCommand OpenMusicGalleryWindowCommand { get; }
        IRelayCommand OpenLyricsWindowSwitchWindowCommand { get; }
        IRelayCommand OpenLyricsSearchWindowCommand { get; }
        IRelayCommand OpenLyricsShareWindowCommand { get; }
        IRelayCommand OpenStatsDashboardWindowCommand { get; }
    }
}
