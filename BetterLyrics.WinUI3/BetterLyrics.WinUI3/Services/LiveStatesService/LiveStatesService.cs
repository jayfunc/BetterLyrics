using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.SettingsService;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Linq;
using System.Threading.Tasks;

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

            WindowHook.SetIsWorkArea<NowPlayingWindow>(LiveStates.LyricsWindowStatus.IsWorkArea);
            if (LiveStates.LyricsWindowStatus.IsWorkArea)
            {
                WindowHook.UpdateWorkArea<NowPlayingWindow>();
            }
            await Task.Delay(300);

            WindowHook.SetIsShowInSwitchers<NowPlayingWindow>(LiveStates.LyricsWindowStatus.IsShownInSwitchers);
            WindowHook.SetIsAlwaysOnTop<NowPlayingWindow>(LiveStates.LyricsWindowStatus.IsAlwaysOnTop);

            WindowHook.SetIsClickThrough<NowPlayingWindow>(LiveStates.LyricsWindowStatus.IsClickThrough);
            WindowHook.SetIsBorderless<NowPlayingWindow>(LiveStates.LyricsWindowStatus.IsBorderless);

            WindowHook.SetLyricsWindowVisibilityByPlayingStatus(_dispatcherQueue);
            WindowHook.SetTitleBarArea<NowPlayingWindow>(LiveStates.LyricsWindowStatus.TitleBarArea);

            // 下述代码可以删除，但是为了避免给用户造成操作上的疑虑，暂时保留
            if (LiveStates.LyricsWindowStatus.IsWorkArea)
            {
                LiveStates.LyricsWindowStatus.WindowBounds = LiveStates.LyricsWindowStatus.GetWindowBoundsWhenWorkArea();
            }

            WindowHook.MoveAndResize<NowPlayingWindow>(LiveStates.LyricsWindowStatus.WindowBounds);
            LiveStates.LyricsWindowStatus.WindowX = LiveStates.LyricsWindowStatus.WindowBounds.X;
            LiveStates.LyricsWindowStatus.WindowY = LiveStates.LyricsWindowStatus.WindowBounds.Y;
            LiveStates.LyricsWindowStatus.WindowWidth = LiveStates.LyricsWindowStatus.WindowBounds.Width;
            LiveStates.LyricsWindowStatus.WindowHeight = LiveStates.LyricsWindowStatus.WindowBounds.Height;

            LiveStates.LyricsWindowStatus.UpdateDemoWindowAndMonitorBounds();

            LiveStates.IsLyricsWindowStatusRefreshing = false;
        }

        private void LyricsWindowStatus_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(LyricsWindowStatus.IsWorkArea):
                    LiveStates.IsLyricsWindowStatusRefreshing = true;
                    WindowHook.SetIsWorkArea<NowPlayingWindow>(LiveStates.LyricsWindowStatus.IsWorkArea);
                    LiveStates.IsLyricsWindowStatusRefreshing = false;
                    if (LiveStates.LyricsWindowStatus.IsWorkArea)
                    {
                        WindowHook.MoveAndResize<NowPlayingWindow>(LiveStates.LyricsWindowStatus.GetWindowBoundsWhenWorkArea());
                    }
                    break;
                case nameof(LyricsWindowStatus.DockHeight):
                case nameof(LyricsWindowStatus.DockPlacement):
                case nameof(LyricsWindowStatus.MonitorDeviceName):
                    LiveStates.LyricsWindowStatus.UpdateMonitorBounds();
                    if (LiveStates.LyricsWindowStatus.IsWorkArea)
                    {
                        LiveStates.IsLyricsWindowStatusRefreshing = true;
                        WindowHook.UpdateWorkArea<NowPlayingWindow>();
                        LiveStates.IsLyricsWindowStatusRefreshing = false;
                        WindowHook.MoveAndResize<NowPlayingWindow>(LiveStates.LyricsWindowStatus.GetWindowBoundsWhenWorkArea());
                    }
                    break;
                case nameof(LyricsWindowStatus.IsShownInSwitchers):
                    WindowHook.SetIsShowInSwitchers<NowPlayingWindow>(LiveStates.LyricsWindowStatus.IsShownInSwitchers);
                    break;
                case nameof(LyricsWindowStatus.IsAlwaysOnTop):
                    WindowHook.SetIsAlwaysOnTop<NowPlayingWindow>(LiveStates.LyricsWindowStatus.IsAlwaysOnTop);
                    break;
                case nameof(LyricsWindowStatus.IsClickThrough):
                    WindowHook.SetIsClickThrough<NowPlayingWindow>(LiveStates.LyricsWindowStatus.IsClickThrough);
                    break;
                case nameof(LyricsWindowStatus.IsBorderless):
                    WindowHook.SetIsBorderless<NowPlayingWindow>(LiveStates.LyricsWindowStatus.IsBorderless);
                    break;
                case nameof(LyricsWindowStatus.WindowX):
                    WindowHook.MoveAndResize<NowPlayingWindow>(LiveStates.LyricsWindowStatus.WindowBounds.WithX(LiveStates.LyricsWindowStatus.WindowX));
                    break;
                case nameof(LyricsWindowStatus.WindowY):
                    WindowHook.MoveAndResize<NowPlayingWindow>(LiveStates.LyricsWindowStatus.WindowBounds.WithY(LiveStates.LyricsWindowStatus.WindowY));
                    break;
                case nameof(LyricsWindowStatus.WindowWidth):
                    WindowHook.MoveAndResize<NowPlayingWindow>(LiveStates.LyricsWindowStatus.WindowBounds.WithWidth(LiveStates.LyricsWindowStatus.WindowWidth));
                    break;
                case nameof(LyricsWindowStatus.WindowHeight):
                    WindowHook.MoveAndResize<NowPlayingWindow>(LiveStates.LyricsWindowStatus.WindowBounds.WithHeight(LiveStates.LyricsWindowStatus.WindowHeight));
                    break;
                case nameof(LyricsWindowStatus.TitleBarArea):
                    WindowHook.SetTitleBarArea<NowPlayingWindow>(LiveStates.LyricsWindowStatus.TitleBarArea);
                    break;
                case nameof(LyricsWindowStatus.AutoShowOrHideWindow):
                    WindowHook.SetLyricsWindowVisibilityByPlayingStatus(_dispatcherQueue);
                    break;
                default:
                    break;
            }
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
