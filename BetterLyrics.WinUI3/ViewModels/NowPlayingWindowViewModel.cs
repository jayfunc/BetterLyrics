// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Diagnostics;

namespace BetterLyrics.WinUI3
{
    public partial class NowPlayingWindowViewModel : BaseWindowViewModel
    {
        private readonly ISettingsService _settingsService;

        public NowPlayingWindowViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;

            AppSettings = _settingsService.AppSettings;
        }

        [ObservableProperty] public partial AppSettings AppSettings { get; set; }

        [ObservableProperty] public partial double TopCommandGridOpacity { get; set; } = 0;

        [ObservableProperty] public partial double TitleBarFontSize { get; set; } = 12;

    }
}
