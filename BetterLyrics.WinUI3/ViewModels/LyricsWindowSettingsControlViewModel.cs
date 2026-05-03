using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Linq;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsWindowSettingsControlViewModel : BaseViewModel,
        IRecipient<PropertyChangedMessage<bool>>
    {
        private readonly ISettingsService _settingsService;

        [ObservableProperty]
        public partial AppSettings AppSettings { get; set; }

        [ObservableProperty]
        public partial object SelectorBarSelectedItemTag { get; set; } = "AlbumArtStyle";

        [ObservableProperty]
        public partial bool IsConfigPanelOpened { get; set; } = false;

        public LyricsWindowSettingsControlViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;

            AppSettings = _settingsService.AppSettings;
        }

        [RelayCommand]
        private void CreateLyricsWindowStatus(LyricsWindowMode mode)
        {
            var status = new LyricsWindowStatus(mode);
            status.LayoutProfileId = AppSettings.LayoutProfiles.First(x => x.Mode == status.GetDefaultLayoutProfileMode()).Id;
            AppSettings.WindowBoundsRecords.Add(status);
        }

        public void OpenConfigPanel()
        {
            IsConfigPanelOpened = true;
        }

        [RelayCommand]
        private void CloseConfigPanel()
        {
            IsConfigPanelOpened = false;
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is GeneralSettings)
            {
                if (message.PropertyName == nameof(GeneralSettings.MultiNowPlayingWindowMode))
                {
                    if (!message.NewValue && AppSettings.WindowBoundsRecords.Any(x => x.WindowStatus == Enums.WindowStatus.Opened))
                    {
                        var windows = WindowHook.GetWindows<NowPlayingWindow>();
                        var latest = windows.Last();
                        foreach (var item in windows)
                        {
                            if (item != latest)
                            {
                                item.CloseWindow();
                            }
                        }
                    }
                }
            }
        }
    }
}
