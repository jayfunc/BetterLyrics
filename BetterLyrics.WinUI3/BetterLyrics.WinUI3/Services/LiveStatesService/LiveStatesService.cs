using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.WinUI.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;

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

        private async void LyricsWindowStatus_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            LiveStates.IsLyricsWindowStatusRefreshing = true;

            switch (e.PropertyName)
            {
                case nameof(LyricsWindowStatus.IsWorkArea):
                    WindowHelper.SetIsWorkArea<LyricsWindow>(LiveStates.LyricsWindowStatus.IsWorkArea);
                    if (LiveStates.LyricsWindowStatus.IsWorkArea)
                    {
                        await Task.Delay(300);
                        WindowHelper.MoveAndResize<LyricsWindow>(GetWindowBoundsWhenWorkArea());
                    }
                    break;
                case nameof(LyricsWindowStatus.DockHeight):
                case nameof(LyricsWindowStatus.DockPlacement):
                    if (LiveStates.LyricsWindowStatus.IsWorkArea)
                    {
                        WindowHelper.UpdateWorkArea<LyricsWindow>();
                        await Task.Delay(300);
                        WindowHelper.MoveAndResize<LyricsWindow>(GetWindowBoundsWhenWorkArea());
                    }
                    break;
                case nameof(LyricsWindowStatus.MonitorDeviceName):
                    // 记录切换前的窗口状态是否沾满屏幕
                    bool isStretchedToMonitor = LiveStates.LyricsWindowStatus.WindowBounds == LiveStates.LyricsWindowStatus.MonitorBounds;
                    LiveStates.LyricsWindowStatus.UpdateMonitorBounds();
                    if (LiveStates.LyricsWindowStatus.IsWorkArea)
                    {
                        WindowHelper.UpdateWorkArea<LyricsWindow>();
                        await Task.Delay(300);
                        WindowHelper.MoveAndResize<LyricsWindow>(GetWindowBoundsWhenWorkArea());
                    }
                    else
                    {
                        if (isStretchedToMonitor)
                        {
                            WindowHelper.MoveAndResize<LyricsWindow>(LiveStates.LyricsWindowStatus.MonitorBounds);
                        }
                        else
                        {
                            WindowHelper.MoveAndResize<LyricsWindow>(new Rect(
                                LiveStates.LyricsWindowStatus.MonitorBounds.X,
                                LiveStates.LyricsWindowStatus.MonitorBounds.Y,
                                Math.Min(LiveStates.LyricsWindowStatus.MonitorBounds.Width, LiveStates.LyricsWindowStatus.WindowBounds.Width),
                                Math.Min(LiveStates.LyricsWindowStatus.MonitorBounds.Height, LiveStates.LyricsWindowStatus.WindowBounds.Height)
                            ));
                        }
                    }
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
                case nameof(LyricsWindowStatus.WindowX):
                    WindowHelper.MoveAndResize<LyricsWindow>(LiveStates.LyricsWindowStatus.WindowBounds.WithX(LiveStates.LyricsWindowStatus.WindowX));
                    break;
                case nameof(LyricsWindowStatus.WindowY):
                    WindowHelper.MoveAndResize<LyricsWindow>(LiveStates.LyricsWindowStatus.WindowBounds.WithY(LiveStates.LyricsWindowStatus.WindowY));
                    break;
                case nameof(LyricsWindowStatus.WindowWidth):
                    WindowHelper.MoveAndResize<LyricsWindow>(LiveStates.LyricsWindowStatus.WindowBounds.WithWidth(LiveStates.LyricsWindowStatus.WindowWidth));
                    break;
                case nameof(LyricsWindowStatus.WindowHeight):
                    WindowHelper.MoveAndResize<LyricsWindow>(LiveStates.LyricsWindowStatus.WindowBounds.WithHeight(LiveStates.LyricsWindowStatus.WindowHeight));
                    break;
                case nameof(LyricsWindowStatus.WindowBounds):
                    LiveStates.LyricsWindowStatus.UpdateMonitorNameAndBounds();
                    LiveStates.LyricsWindowStatus.UpdateDemoWindowAndMonitorBounds();
                    LiveStates.LyricsWindowStatus.WindowX = LiveStates.LyricsWindowStatus.WindowBounds.X;
                    LiveStates.LyricsWindowStatus.WindowY = LiveStates.LyricsWindowStatus.WindowBounds.Y;
                    LiveStates.LyricsWindowStatus.WindowWidth = LiveStates.LyricsWindowStatus.WindowBounds.Width;
                    LiveStates.LyricsWindowStatus.WindowHeight = LiveStates.LyricsWindowStatus.WindowBounds.Height;
                    break;
                case nameof(LyricsWindowStatus.TitleBarArea):
                    WindowHelper.SetTitleBarArea<LyricsWindow>(LiveStates.LyricsWindowStatus.TitleBarArea);
                    break;
                default:
                    break;
            }

            LiveStates.IsLyricsWindowStatusRefreshing = true;
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

        public async void RefreshLyricsWindowStatus()
        {
            LiveStates.IsLyricsWindowStatusRefreshing = true;

            LiveStates.LyricsWindowStatus.UpdateMonitorBounds();

            WindowHelper.SetIsWorkArea<LyricsWindow>(LiveStates.LyricsWindowStatus.IsWorkArea);
            await Task.Delay(300);

            WindowHelper.SetIsShowInSwitchers<LyricsWindow>(LiveStates.LyricsWindowStatus.IsShownInSwitchers);
            WindowHelper.SetIsAlwaysOnTop<LyricsWindow>(LiveStates.LyricsWindowStatus.IsAlwaysOnTop);
            WindowHelper.SetIsClickThrough<LyricsWindow>(LiveStates.LyricsWindowStatus.IsClickThrough);
            WindowHelper.SetIsBorderless<LyricsWindow>(LiveStates.LyricsWindowStatus.IsBorderless);
            WindowHelper.SetLyricsWindowVisibilityByPlayingStatus();
            WindowHelper.SetTitleBarArea<LyricsWindow>(LiveStates.LyricsWindowStatus.TitleBarArea);

            if (LiveStates.LyricsWindowStatus.IsWorkArea)
            {
                LiveStates.LyricsWindowStatus.WindowBounds = GetWindowBoundsWhenWorkArea();
            }

            WindowHelper.MoveAndResize<LyricsWindow>(LiveStates.LyricsWindowStatus.WindowBounds);

            LiveStates.LyricsWindowStatus.UpdateDemoWindowAndMonitorBounds();

            LiveStates.IsLyricsWindowStatusRefreshing = false;
        }

        private Rect GetWindowBoundsWhenWorkArea()
        {
            return new Rect(
                LiveStates.LyricsWindowStatus.MonitorBounds.X,
                LiveStates.LyricsWindowStatus.DockPlacement switch
                {
                    Enums.DockPlacement.Top => LiveStates.LyricsWindowStatus.MonitorBounds.Top,
                    Enums.DockPlacement.Bottom => LiveStates.LyricsWindowStatus.MonitorBounds.Bottom - LiveStates.LyricsWindowStatus.DockHeight - 1,
                    _ => LiveStates.LyricsWindowStatus.MonitorBounds.Top,
                },
                LiveStates.LyricsWindowStatus.MonitorBounds.Width,
                LiveStates.LyricsWindowStatus.DockPlacement switch
                {
                    Enums.DockPlacement.Top => LiveStates.LyricsWindowStatus.DockHeight,
                    Enums.DockPlacement.Bottom => LiveStates.LyricsWindowStatus.DockHeight + 1,
                    _ => LiveStates.LyricsWindowStatus.DockHeight,
                }
            );
        }
    }
}
