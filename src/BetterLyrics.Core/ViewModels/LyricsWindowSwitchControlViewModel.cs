using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.ViewModels;

public partial class LyricsWindowSwitchControlViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;

    public LyricsWindowSwitchControlViewModel(ISettingsService settingsService, INavigationService navigationService)
    {
        _settingsService = settingsService;
        NavigationService = navigationService;
        AppSettings = _settingsService.AppSettings;
    }

    private INavigationService NavigationService { get; }

    [ObservableProperty] public partial AppSettings AppSettings { get; set; }
}