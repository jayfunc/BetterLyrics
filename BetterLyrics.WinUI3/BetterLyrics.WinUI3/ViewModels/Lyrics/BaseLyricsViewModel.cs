using BetterLyrics.WinUI3.Services.Settings;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class BaseLyricsViewModel : BaseSettingsViewModel
    {
        public BaseLyricsViewModel(ISettingsService settingsService)
            : base(settingsService) { }
    }
}
