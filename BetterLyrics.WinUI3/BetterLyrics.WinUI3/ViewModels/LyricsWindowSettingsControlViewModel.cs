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
using System.Numerics;

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

        [ObservableProperty]
        public partial Vector3 ConfigPanelTranslation { get; set; } = new();

        [ObservableProperty]
        public partial double DisplayPanelHeight { get; set; } = 0;

        public LyricsWindowSettingsControlViewModel(ISettingsService settingsService)
        {
            _settingsService = settingsService;

            AppSettings = _settingsService.AppSettings;
        }

        [RelayCommand]
        private void CreateStandardLyricsWindowStatus()
        {
            AppSettings.WindowBoundsRecords.Add(LyricsWindowStatusExtensions.StandardMode());
        }

        [RelayCommand]
        private void CreateTransparentLyricsWindowStatus()
        {
            AppSettings.WindowBoundsRecords.Add(LyricsWindowStatusExtensions.DesktopMode());
        }

        [RelayCommand]
        private void CreateDockedLyricsWindowStatus()
        {
            AppSettings.WindowBoundsRecords.Add(LyricsWindowStatusExtensions.DockedMode());
        }

        [RelayCommand]
        private void CreateFullLyricsWindowStatus()
        {
            AppSettings.WindowBoundsRecords.Add(LyricsWindowStatusExtensions.FullscreenMode());
        }

        [RelayCommand]
        private void CreateNarrowLyricsWindowStatus()
        {
            AppSettings.WindowBoundsRecords.Add(LyricsWindowStatusExtensions.NarrowMode());
        }

        [RelayCommand]
        private void CreateTaskbarLyricsWindowStatus()
        {
            AppSettings.WindowBoundsRecords.Add(LyricsWindowStatusExtensions.TaskbarMode());
        }

        public void OpenConfigPanel()
        {
            IsConfigPanelOpened = true;
            ConfigPanelTranslation = new();
        }

        [RelayCommand]
        private void CloseConfigPanel()
        {
            IsConfigPanelOpened = false;
            ConfigPanelTranslation = new(0, (float)DisplayPanelHeight, 0);
        }

        partial void OnDisplayPanelHeightChanged(double value)
        {
            if (IsConfigPanelOpened)
            {
                OpenConfigPanel();
            }
            else
            {
                CloseConfigPanel();
            }
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is GeneralSettings)
            {
                if (message.PropertyName == nameof(GeneralSettings.MultiNowPlayingWindowMode))
                {
                    if (!message.NewValue && AppSettings.WindowBoundsRecords.Count(x => x.IsOpened) > 0)
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
