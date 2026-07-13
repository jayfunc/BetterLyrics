using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterLyrics.Core.ViewModels;

public partial class PlayQueueViewModel : BaseViewModel
{
    private readonly ISettingsService _settingsService;

    public PlayQueueViewModel(ISmtcService smtcService, ISettingsService settingsService)
    {
        _settingsService = settingsService;
        SMTCService = smtcService;
        AppSettings = _settingsService.AppSettings;
    }

    public ISmtcService SMTCService { get; set; }

    [ObservableProperty] public partial AppSettings AppSettings { get; set; }
}