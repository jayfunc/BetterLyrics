using BetterLyrics.WinUI3.Services;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class SettingsWindowViewModel(ISettingsService settingsService) : BaseWindowViewModel(settingsService)
    {
    }
}
