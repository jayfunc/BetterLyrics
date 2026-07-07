using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BetterLyrics.Core.ViewModels;

public partial class LyricsWindowSettingsControlViewModel : BaseViewModel,
    IRecipient<PropertyChangedMessage<bool>>
{
    private readonly ISettingsService _settingsService;
    private readonly IWindowManagerProvider _windowManagerProvider;

    public LyricsWindowSettingsControlViewModel(ISettingsService settingsService,
        IWindowManagerProvider windowManagerProvider)
    {
        _settingsService = settingsService;
        _windowManagerProvider = windowManagerProvider;

        AppSettings = _settingsService.AppSettings;
    }

    [ObservableProperty] public partial AppSettings AppSettings { get; set; }

    [ObservableProperty] public partial object SelectorBarSelectedItemTag { get; set; } = "AlbumArtStyle";

    public void Receive(PropertyChangedMessage<bool> message)
    {
        if (message.Sender is GeneralSettings)
            if (message.PropertyName == nameof(GeneralSettings.MultiNowPlayingWindowMode))
                if (!message.NewValue &&
                    AppSettings.WindowBoundsRecords.Any(x => x.WindowStatus == WindowStatus.Opened))
                {
                    var windows = _windowManagerProvider.GetWindows(WindowType.NowPlayingWindow);
                    var latest = windows.Last();
                    foreach (var item in windows)
                        if (item != latest)
                            _windowManagerProvider.CloseWindow(item);
                }
    }

    [RelayCommand]
    private void CreateLyricsWindowStatus(LyricsWindowMode mode)
    {
        var status = new LyricsWindowStatus(mode);
        status.LayoutProfileId =
            AppSettings.LayoutProfiles.First(x => x.Mode == status.GetDefaultLayoutProfileMode()).Id;
        AppSettings.WindowBoundsRecords.Add(status);
    }
}