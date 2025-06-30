using BetterLyrics.WinUI3.Services;

namespace BetterLyrics.WinUI3.ViewModels
{
    public class SettingsWindowViewModel : BaseWindowViewModel
    {
        public SettingsWindowViewModel(ISettingsService settingsService) : base(settingsService) { }
    }
}
