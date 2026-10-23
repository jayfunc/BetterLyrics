using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using BetterLyrics.Core.Interfaces.ViewModels;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// Transient ViewModel
/// </summary>
public partial class LyricsWindowSwitchControlViewModel : ObservableObject, ITransientViewModel
{
    private readonly ISettingsService _settingsService;

    private INavigationService NavigationService { get; }

    [ObservableProperty] public partial AppSettings AppSettings { get; set; }

    public LyricsWindowSwitchControlViewModel(ISettingsService settingsService, INavigationService navigationService)
    {
        _settingsService = settingsService;
        NavigationService = navigationService;
        AppSettings = _settingsService.AppSettings;
    }
}