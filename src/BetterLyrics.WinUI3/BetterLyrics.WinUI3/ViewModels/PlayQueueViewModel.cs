using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Core.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class PlayQueueViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;

        public ISMTCService SMTCService { get; set; }

        [ObservableProperty] public partial AppSettings AppSettings { get; set; }

        public PlayQueueViewModel(ISMTCService smtcService, ISettingsService settingsService)
        {
            _settingsService = settingsService;
            SMTCService = smtcService;
            AppSettings = _settingsService.AppSettings;
        }
    }
}
