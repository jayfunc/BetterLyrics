using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.LiveStatesService;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsWindowSwitchControlViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly ILiveStatesService _liveStatesService;

        [ObservableProperty]
        public partial LiveStates LiveStates { get; set; }


        [ObservableProperty]
        public partial AppSettings AppSettings { get; set; }

        public LyricsWindowSwitchControlViewModel(ISettingsService settingsService, ILiveStatesService liveStatesService)
        {
            _settingsService = settingsService;
            _liveStatesService = liveStatesService;
            AppSettings = _settingsService.AppSettings;
            LiveStates = _liveStatesService.LiveStates;
        }

    }
}
