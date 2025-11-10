using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI.Controls;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;

namespace BetterLyrics.WinUI3.Services.LiveStatesService
{
    public partial class LiveStatesService : BaseViewModel, ILiveStatesService,
        IRecipient<PropertyChangedMessage<LyricsWindowStatus>>
    {
        private readonly ISettingsService _settingsService;

        public LiveStates LiveStates { get; set; } = new LiveStates();

        public LiveStatesService(ISettingsService settingsService)
        {
            _settingsService = settingsService;
        }

        public void InitLyricsWindowStatus()
        {
            var defaultLyricsWindowStatus = _settingsService.AppSettings.WindowBoundsRecords.FirstOrDefault(x => x.IsDefault);
            if (defaultLyricsWindowStatus == null)
            {
                defaultLyricsWindowStatus = LyricsWindowStatusExtensions.StandardMode();
                defaultLyricsWindowStatus.IsDefault = true;
                _settingsService.AppSettings.WindowBoundsRecords.Add(defaultLyricsWindowStatus);
                _settingsService.AppSettings.WindowBoundsRecords.Add(LyricsWindowStatusExtensions.DesktopMode());
                _settingsService.AppSettings.WindowBoundsRecords.Add(LyricsWindowStatusExtensions.DockedMode());
                _settingsService.AppSettings.WindowBoundsRecords.Add(LyricsWindowStatusExtensions.NarrowMode());
                _settingsService.AppSettings.WindowBoundsRecords.Add(LyricsWindowStatusExtensions.FullscreenMode());
            }
            LiveStates.LyricsWindowStatus = defaultLyricsWindowStatus;
        }

        private async void RefreshLyricsWindowStatus()
        {
            LiveStates.IsLyricsWindowStatusRefreshing = true;

            LiveStates.LyricsWindowStatus.UpdateMonitorBounds();

            WindowHelper.SetIsWorkArea<LyricsWindow>(LiveStates.LyricsWindowStatus.IsWorkArea);
            await Task.Delay(300);

            WindowHelper.SetIsShowInSwitchers<LyricsWindow>(LiveStates.LyricsWindowStatus.IsShownInSwitchers);
            WindowHelper.SetIsAlwaysOnTop<LyricsWindow>(LiveStates.LyricsWindowStatus.IsAlwaysOnTop);

            WindowHelper.SetIsClickThrough<LyricsWindow>(LiveStates.LyricsWindowStatus.IsClickThrough);
            WindowHelper.SetIsBorderless<LyricsWindow>(LiveStates.LyricsWindowStatus.IsBorderless);

            WindowHelper.SetLyricsWindowVisibilityByPlayingStatus(_dispatcherQueue);
            WindowHelper.SetTitleBarArea<LyricsWindow>(LiveStates.LyricsWindowStatus.TitleBarArea);

            // 下述代码可以删除，但是为了避免给用户造成操作上的疑虑，暂时保留
            if (LiveStates.LyricsWindowStatus.IsWorkArea)
            {
                LiveStates.LyricsWindowStatus.WindowBounds = LiveStates.LyricsWindowStatus.GetWindowBoundsWhenWorkArea();
            }

            WindowHelper.MoveAndResize<LyricsWindow>(LiveStates.LyricsWindowStatus.WindowBounds);
            LiveStates.LyricsWindowStatus.WindowX = LiveStates.LyricsWindowStatus.WindowBounds.X;
            LiveStates.LyricsWindowStatus.WindowY = LiveStates.LyricsWindowStatus.WindowBounds.Y;
            LiveStates.LyricsWindowStatus.WindowWidth = LiveStates.LyricsWindowStatus.WindowBounds.Width;
            LiveStates.LyricsWindowStatus.WindowHeight = LiveStates.LyricsWindowStatus.WindowBounds.Height;

            LiveStates.LyricsWindowStatus.UpdateDemoWindowAndMonitorBounds();

            LiveStates.IsLyricsWindowStatusRefreshing = false;
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
                        WindowHelper.MoveAndResize<LyricsWindow>(LiveStates.LyricsWindowStatus.GetWindowBoundsWhenWorkArea());
                    }
                    break;
                case nameof(LyricsWindowStatus.DockHeight):
                case nameof(LyricsWindowStatus.DockPlacement):
                case nameof(LyricsWindowStatus.MonitorDeviceName):
                    LiveStates.LyricsWindowStatus.UpdateMonitorBounds();
                    if (LiveStates.LyricsWindowStatus.IsWorkArea)
                    {
                        WindowHelper.UpdateWorkArea<LyricsWindow>();
                        await Task.Delay(300);
                        WindowHelper.MoveAndResize<LyricsWindow>(LiveStates.LyricsWindowStatus.GetWindowBoundsWhenWorkArea());
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
                case nameof(LyricsWindowStatus.TitleBarArea):
                    WindowHelper.SetTitleBarArea<LyricsWindow>(LiveStates.LyricsWindowStatus.TitleBarArea);
                    break;
                case nameof(LyricsWindowStatus.AutoShowOrHideWindow):
                    WindowHelper.SetLyricsWindowVisibilityByPlayingStatus(_dispatcherQueue);
                    break;
                default:
                    break;
            }

            LiveStates.IsLyricsWindowStatusRefreshing = false;
        }

        public void Receive(PropertyChangedMessage<LyricsWindowStatus> message)
        {
            if (message.Sender is LiveStates)
            {
                if (message.PropertyName == nameof(LiveStates.LyricsWindowStatus))
                {
                    message.OldValue.PropertyChanged -= LyricsWindowStatus_PropertyChanged;
                    message.NewValue.PropertyChanged += LyricsWindowStatus_PropertyChanged;
                    RefreshLyricsWindowStatus();
                }
            }
        }
    }
}
