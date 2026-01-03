using BetterLyrics.WinUI3.Controls;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Services.SMTCService;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class PlayQueueViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;

        public ISMTCService SMTCService { get; set; }

        [ObservableProperty] public partial AppSettings AppSettings { get; set; }

        public PlayQueueViewModel(ISMTCService smtcService, ISettingsService settingsService)
        {
            _settingsService = settingsService;
            SMTCService = smtcService;
            AppSettings = _settingsService.AppSettings;
        }
    }
}
