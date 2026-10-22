using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using BetterLyrics.Core.Interfaces.ViewModels;

namespace BetterLyrics.Core.ViewModels;

/// <summary>
/// Transient ViewModel，生命周期跟随 MusicGalleryWindow 实例。
/// </summary>
public partial class MusicGalleryWindowViewModel : ObservableObject, ITransientViewModel
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty] public partial AppSettings AppSettings { get; set; }

    public MusicGalleryWindowViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        AppSettings = _settingsService.AppSettings;
    }
}