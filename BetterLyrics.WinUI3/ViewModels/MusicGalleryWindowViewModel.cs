using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class MusicGalleryWindowViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;

        [ObservableProperty]
        public partial AppSettings AppSettings { get; set; }

        public MusicGalleryWindowViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;

            AppSettings = _settingsService.AppSettings;
        }

    }
}
