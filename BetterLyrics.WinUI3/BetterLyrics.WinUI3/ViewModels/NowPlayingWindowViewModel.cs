// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using Windows.UI;
using WinRT.Interop;
using WinUIEx;

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

        [ObservableProperty] public partial double TitleBarFontSize { get; set; } = 14;

    }
}
