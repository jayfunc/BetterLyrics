using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.NavigationService;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsWindowSwitchControlViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private INavigationService NavigationService { get; }

        [ObservableProperty]
        public partial AppSettings AppSettings { get; set; }

        public LyricsWindowSwitchControlViewModel(ISettingsService settingsService, INavigationService navigationService)
        {
            _settingsService = settingsService;
            NavigationService = navigationService;
            AppSettings = _settingsService.AppSettings;
        }

    }
}
