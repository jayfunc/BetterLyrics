using CommunityToolkit.Mvvm.Input;

namespace BetterLyrics.Core.Interfaces.Services;

public interface INavigationService
{
    IRelayCommand OpenSettingsWindowCommand { get; }
    IRelayCommand OpenMusicGalleryWindowCommand { get; }
    IRelayCommand OpenLyricsWindowSwitchWindowCommand { get; }
    IRelayCommand OpenLyricsSearchWindowCommand { get; }
    IRelayCommand OpenLyricsShareWindowCommand { get; }
    IRelayCommand OpenStatsDashboardWindowCommand { get; }
}