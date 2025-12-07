using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsWindowSettingsControlViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;

        [ObservableProperty]
        public partial AppSettings AppSettings { get; set; }

        [ObservableProperty]
        public partial object ListViewSelectedItemTag { get; set; } = "General";

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
    }
}
