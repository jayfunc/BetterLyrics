using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.LiveStatesService;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Vanara.PInvoke.User32.RAWINPUT;

namespace BetterLyrics.WinUI3.ViewModels
{
    public partial class LyricsWindowSettingsControlViewModel : BaseViewModel
    {
        private readonly ISettingsService _settingsService;
        private readonly ILiveStatesService _liveStatesService;

        [ObservableProperty]
        public partial LiveStates LiveStates { get; set; }

        [ObservableProperty]
        public partial AppSettings AppSettings { get; set; }

        [ObservableProperty]
        public partial object ListViewSelectedItemTag { get; set; } = "General";

        [ObservableProperty]
        public partial ObservableCollection<string> MonitorDeviceNames { get; set; }

        public LyricsWindowSettingsControlViewModel(ISettingsService settingsService, ILiveStatesService liveStatesService)
        {
            _settingsService = settingsService;
            _liveStatesService = liveStatesService;
            AppSettings = _settingsService.AppSettings;
            AppSettings.WindowBoundsRecords.CollectionChanged += WindowBoundsRecords_CollectionChanged;
            LiveStates = _liveStatesService.LiveStates;
            MonitorDeviceNames = [.. MonitorHelper.GetAllMonitorDeviceNames()];
        }

        private void WindowBoundsRecords_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (!AppSettings.WindowBoundsRecords.Any(x => x == LiveStates.LyricsWindowStatus))
            {
                LiveStates.LyricsWindowStatus = AppSettings.WindowBoundsRecords.FirstOrDefault();
            }
        }

        [RelayCommand]
        private void RefreshMonitorDeviceNames()
        {
            MonitorDeviceNames = [.. MonitorHelper.GetAllMonitorDeviceNames()];
            LiveStates.LyricsWindowStatus.MonitorDeviceName = MonitorDeviceNames.FirstOrDefault() ?? "";
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
            AppSettings.WindowBoundsRecords.Add(LyricsWindowStatusExtensions.FullscreenMode(LiveStates.LyricsWindowStatus.MonitorBounds));
        }

        [RelayCommand]
        private void CreateNarrowLyricsWindowStatus()
        {
            AppSettings.WindowBoundsRecords.Add(LyricsWindowStatusExtensions.NarrowMode());
        }

        [RelayCommand]
        private void CopyLyricsWindowStatus()
        {
            var data = (LyricsWindowStatus)LiveStates.LyricsWindowStatus.Clone();
            data.IsDefault = false;
            AppSettings.WindowBoundsRecords.Add(data);
        }
    }
}
