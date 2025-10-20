using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using System.Linq;

namespace BetterLyrics.WinUI3.Services.LiveStatesService
{
    public partial class LiveStatesService : BaseViewModel, ILiveStatesService
    {
        private readonly ISettingsService _settingsService;

        public LiveStates LiveStates { get; set; } = new();

        public LiveStatesService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
            LiveStates.PropertyChanged += LiveStates_PropertyChanged;
            LiveStates.PropertyChanging += LiveStates_PropertyChanging;
            InitLyricsWindowStatus();
        }

        private void LiveStates_PropertyChanging(object? sender, System.ComponentModel.PropertyChangingEventArgs e)
        {
            if (e.PropertyName == nameof(LiveStates.LyricsWindowStatus))
            {
                LiveStates.LyricsWindowStatus.PropertyChanged -= LyricsWindowStatus_PropertyChanged;
            }
        }

        private void LyricsWindowStatus_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(LyricsWindowStatus.DockHeight):
                case nameof(LyricsWindowStatus.IsWorkArea):
                case nameof(LyricsWindowStatus.DockPlacement):
                case nameof(LyricsWindowStatus.MonitorDeviceName):
                    WindowHelper.UpdateWorkAreaHeight<LyricsWindow>();
                    break;
                case nameof(LyricsWindowStatus.IsShownInSwitchers):
                    WindowHelper.SetIsShowInSwitchers<LyricsWindow>(LiveStates.LyricsWindowStatus.IsShownInSwitchers);
                    break;
                case nameof(LyricsWindowStatus.IsAlwaysOnTop):
                    WindowHelper.SetIsAlwaysOnTop<LyricsWindow>(LiveStates.LyricsWindowStatus.IsAlwaysOnTop);
                    break;
                case nameof(LyricsWindowStatus.IsClickThrough):
                    WindowHelper.SetIsClickThrough<LyricsWindow>(LiveStates.LyricsWindowStatus.IsClickThrough);
                    break;
                case nameof(LyricsWindowStatus.IsBorderless):
                    WindowHelper.SetIsBorderless<LyricsWindow>(LiveStates.LyricsWindowStatus.IsBorderless);
                    break;
                case nameof(LyricsWindowStatus.WindowBounds):
                    WindowHelper.MoveAndResize<LyricsWindow>(LiveStates.LyricsWindowStatus.WindowBounds);
                    break;
                case nameof(LyricsWindowStatus.TitleBarArea):
                    WindowHelper.SetTitleBarArea<LyricsWindow>(LiveStates.LyricsWindowStatus.TitleBarArea);
                    break;
                default:
                    break;
            }
        }

        private void LiveStates_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LiveStates.LyricsWindowStatus))
            {
                LiveStates.LyricsWindowStatus.PropertyChanged += LyricsWindowStatus_PropertyChanged;
                RefreshLyricsWindowStatus();
            }
        }

        private void InitLyricsWindowStatus()
        {
            var defaultLyricsWindowStatus = _settingsService.AppSettings.WindowBoundsRecords.FirstOrDefault(x => x.IsDefault);
            if (defaultLyricsWindowStatus == null)
            {
                defaultLyricsWindowStatus = LyricsWindowStatusExtensions.StandardMode();
                defaultLyricsWindowStatus.IsDefault = true;
                _settingsService.AppSettings.WindowBoundsRecords.Add(defaultLyricsWindowStatus);
            }
            LiveStates.LyricsWindowStatus = defaultLyricsWindowStatus;
        }

        public void RefreshLyricsWindowStatus()
        {
            // Order matters!!!
            WindowHelper.SetIsWorkArea<LyricsWindow>(LiveStates.LyricsWindowStatus.IsWorkArea);
            WindowHelper.SetIsShowInSwitchers<LyricsWindow>(LiveStates.LyricsWindowStatus.IsShownInSwitchers);
            WindowHelper.SetIsAlwaysOnTop<LyricsWindow>(LiveStates.LyricsWindowStatus.IsAlwaysOnTop);
            WindowHelper.SetIsClickThrough<LyricsWindow>(LiveStates.LyricsWindowStatus.IsClickThrough);
            WindowHelper.SetIsBorderless<LyricsWindow>(LiveStates.LyricsWindowStatus.IsBorderless);
            WindowHelper.SetLyricsWindowVisibilityByPlayingStatus();
            WindowHelper.SetTitleBarArea<LyricsWindow>(LiveStates.LyricsWindowStatus.TitleBarArea);
            WindowHelper.MoveAndResize<LyricsWindow>(LiveStates.LyricsWindowStatus.WindowBounds);
            LiveStates.LyricsWindowStatus.UpdateMonitorNameAndBounds();
            LiveStates.LyricsWindowStatus.UpdateDemoWindowAndMonitorBounds();
        }
    }
}
