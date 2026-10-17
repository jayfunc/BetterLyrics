using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// Singleton ViewModel
/// </summary>
public partial class MusicGalleryWindowViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty] public partial AppSettings AppSettings { get; set; }

    public MusicGalleryWindowViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        AppSettings = _settingsService.AppSettings;
    }
}