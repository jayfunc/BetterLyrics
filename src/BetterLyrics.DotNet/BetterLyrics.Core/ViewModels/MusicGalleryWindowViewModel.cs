using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.ViewModels;

public partial class MusicGalleryWindowViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;

    public MusicGalleryWindowViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;

        AppSettings = _settingsService.AppSettings;
    }

    [ObservableProperty] public partial AppSettings AppSettings { get; set; }
}