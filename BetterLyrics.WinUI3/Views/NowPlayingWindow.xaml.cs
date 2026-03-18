// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.GSMTCService;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Foundation;
using Windows.UI;
using WinRT.Interop;
using WinUIEx.Messaging;
using static Vanara.PInvoke.User32;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class NowPlayingWindow : Window,
        IRecipient<PropertyChangedMessage<bool>>,
        IRecipient<PropertyChangedMessage<double>>,
        IRecipient<PropertyChangedMessage<string>>,
        IRecipient<PropertyChangedMessage<DockPlacement>>,
        IRecipient<PropertyChangedMessage<TitleBarArea>>,
        IRecipient<PropertyChangedMessage<ElementTheme>>,
        IRecipient<PropertyChangedMessage<BitmapImage?>>,
        IRecipient<PropertyChangedMessage<LyricsFontColorType>>,
        IRecipient<PropertyChangedMessage<Color>>,
        IRecipient<PropertyChangedMessage<TaskbarPlacement>>,
        IRecipient<PropertyChangedMessage<PaletteGeneratorType>>
    {
        private ForegroundWindowHook? _fgWindowWatcher = null;
        private OverlayInputHelper? _overlayInputHelper;
        private TaskbarHook? _taskbarHook;
        private WindowMessageMonitor? _wmm;

        private DispatcherQueueTimer? _fgWindowWatcherTimer = null;

        private Color _backdropAccentColor = Colors.Transparent;

        public LyricsWindowStatus LyricsWindowStatus { get; private set; }

        private readonly IGSMTCService _gsmtcService = Ioc.Default.GetRequiredService<IGSMTCService>();
        private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

        public NowPlayingWindow(LyricsWindowStatus status)
        {
            this.InitializeComponent();
            _wmm = new WindowMessageMonitor(this);
            _wmm.WindowMessageReceived += Wmm_WindowMessageReceived;

            _fgWindowWatcherTimer = DispatcherQueue.CreateTimer();

            LyricsWindowStatus = status;
            NowPlayingPage.LyricsWindowStatus = LyricsWindowStatus;
            NowPlayingBar.LyricsWindowStatus = LyricsWindowStatus;

            this.Init(title: $"{status.Name} - {Constants.App.AppName}", titleBarHeightOption: TitleBarHeightOption.Collapsed, backdropType: BackdropType.Transparent);

            AppWindow.Changed += AppWindow_Changed;
            AppWindow.Closing += AppWindow_Closing;

            WeakReferenceMessenger.Default.RegisterAll(this);

            _ = UpdateAlbumArtThemeColorsAsync();
        }

        private void Wmm_WindowMessageReceived(object? sender, WindowMessageEventArgs e)
        {
            var msg = (WindowMessage)e.Message.MessageId;
            if (msg == WindowMessage.WM_WINDOWPOSCHANGING)
            {
                if (LyricsWindowStatus.IsWorkArea)
                {
                    var pos = Marshal.PtrToStructure<WINDOWPOS>(e.Message.LParam);
                    var bounds = LyricsWindowStatus.GetWindowBoundsWhenWorkArea();
                    pos.x = (int)bounds.X;
                    pos.y = (int)bounds.Y;
                    pos.cx = (int)bounds.Width;
                    pos.cy = (int)bounds.Height;
                    Marshal.StructureToPtr(pos, e.Message.LParam, false);

                    e.Result = IntPtr.Zero;
                    e.Handled = true;
                }
            }
        }

        private void OnTaskbarFreeBoundsChanged(Events.TaskbarFreeBoundsChangedEventArgs obj)
        {
            App.SystemTrayWindow.DispatcherQueue.TryEnqueue(() =>
            {
                this.MoveAndResize(obj.TaskbarFreeBounds);
            });
        }

        public void InitStatus()
        {
            LyricsWindowStatus.UpdateMonitorBounds();

            this.MoveAndResize(LyricsWindowStatus.WindowBounds);
            OnIsShownInSwitchersChanged();
            OnIsAlwaysOnTopChanged();
            OnAutoShowOrHideWindowChanged();
            OnTitleBarAreaChanged();
            OnIsLockedChanged();
            OnIsPinToTaskbarChanged();
            OnIsAlwaysHideUnlockButtonChanged();
            OnIsWorkAreaChanged();
            OnIsMaximizedChanged();
            OnIsFullscreenChanged();

            LyricsWindowStatus.UpdateDemoWindowAndMonitorBounds();
        }

        public void UpdateBackdropAccentColor(nint hwnd)
        {
            var oldValue = _backdropAccentColor;
            var newValue = Helper.ColorHelper.GetAccentColor(
                hwnd,
                LyricsWindowStatus.MonitorDeviceName,
                LyricsWindowStatus.EnvironmentSampleMode);
            // 防止不必要刷新导致界面不流畅
            if (newValue != oldValue)
            {
                _backdropAccentColor = newValue;
                _ = UpdateAlbumArtThemeColorsAsync();
            }
        }

        public void InitFgWindowWatcher()
        {
            var hwnd = WindowNative.GetWindowHandle(this);

            _fgWindowWatcher = new ForegroundWindowHook(
                hwnd,
                fgHwnd =>
                {
                    _fgWindowWatcherTimer?.Debounce(() =>
                    {
                        if (LyricsWindowStatus.IsAlwaysOnTop &&
                            LyricsWindowStatus.IsAlwaysOnTopPolling &&
                            this.AppWindow != null &&
                            this.AppWindow.Presenter is OverlappedPresenter presenter)
                        {
                            presenter.IsAlwaysOnTop = true;
                        }
                        if (LyricsWindowStatus.IsAdaptToEnvironment)
                        {
                            UpdateBackdropAccentColor(hwnd);
                        }
                    }, TimeSpan.FromSeconds(1));
                }
            );
            if (LyricsWindowStatus.IsAdaptToEnvironment)
            {
                UpdateBackdropAccentColor(hwnd);
            }
            OnIsAdaptToEnvironmentChanged();
        }

        private async Task UpdateAlbumArtThemeColorsAsync()
        {
            var result = await _gsmtcService.CalculateAlbumArtThemeColorsAsync(LyricsWindowStatus, _backdropAccentColor);

            NowPlayingPage.LyricsWindowStatus?.WindowPalette = result;
            RootGrid.RequestedTheme = result.ThemeType;
        }

        // ====

        private void OnIsWorkAreaChanged()
        {
            this.SetIsWorkArea(LyricsWindowStatus.IsWorkArea);
            if (LyricsWindowStatus.IsWorkArea)
            {
                LyricsWindowStatus.IsLocked = true;
                this.UpdateBackdropAccentColor(WindowNative.GetWindowHandle(this));
            }
            else
            {
                // 强制触发一次更新，刷新解锁图标可见性状态
                OnIsLockedChanged();
            }
        }

        private void OnIsShownInSwitchersChanged()
        {
            this.SetIsShowInSwitchers(LyricsWindowStatus.IsShownInSwitchers);
        }

        private void OnIsAlwaysOnTopChanged()
        {
            this.SetIsAlwaysOnTop(LyricsWindowStatus.IsAlwaysOnTop);
            PinFillFontIcon.Opacity = LyricsWindowStatus.IsAlwaysOnTop ? 1 : 0;
        }

        private void OnIsLockedChanged()
        {
            if (LyricsWindowStatus.IsLocked)
            {
                LockToggleButtonContainer.Visibility = Visibility.Visible;
                if (LyricsWindowStatus.IsWallpaper)
                {
                    WorkerWHook.PinToDesktop(this);
                }
                else
                {
                    RestartOverlayInputHelper();
                }
            }
            else
            {
                LockToggleButtonContainer.Visibility = Visibility.Collapsed;
                UnlockButton.Opacity = 0;
                if (LyricsWindowStatus.IsWallpaper)
                {
                    WorkerWHook.UnpinFromDesktop(this);
                }
                else
                {
                    StopOverlayInputHelper();
                }
            }

            if (LyricsWindowStatus.IsBorderlessWhenLocked)
            {
                this.SetIsBorderless(LyricsWindowStatus.IsLocked);
            }

            if (!LyricsWindowStatus.IsWallpaper)
            {
                this.SetIsClickThrough(LyricsWindowStatus.IsLocked);
            }
        }

        private void OnIsPinToTaskbarChanged()
        {
            _taskbarHook?.Dispose();
            _taskbarHook = null;

            if (LyricsWindowStatus.IsPinToTaskbar)
            {
                _taskbarHook = new(LyricsWindowStatus.TaskbarPlacement, OnTaskbarFreeBoundsChanged);
            }
        }

        private void OnIsAlwaysHideUnlockButtonChanged()
        {
            UnlockButton.Visibility = LyricsWindowStatus.IsAlwaysHideUnlockButton ? Visibility.Collapsed : Visibility.Visible;
        }

        private void OnIsFullscreenChanged()
        {
            if (this.SetIsFullscreen(LyricsWindowStatus.IsFullscreen))
            {
                EnterFullscreenFontIcon.Opacity = LyricsWindowStatus.IsFullscreen ? 0 : 1;
                ExitFullscreenFontIcon.Opacity = LyricsWindowStatus.IsFullscreen ? 1 : 0;
                MaximizeButton.Visibility = LyricsWindowStatus.IsFullscreen ? Visibility.Collapsed : Visibility.Visible;
                AOTButton.Visibility = LyricsWindowStatus.IsFullscreen ? Visibility.Collapsed : Visibility.Visible;
                MinimizeButton.Visibility = LyricsWindowStatus.IsFullscreen ? Visibility.Collapsed : Visibility.Visible;
                LockButton.Visibility = LyricsWindowStatus.IsFullscreen ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private void OnIsMaximizedChanged()
        {
            if (this.SetIsMaximized(LyricsWindowStatus.IsMaximized))
            {
                EnterMaximizeFontIcon.Opacity = LyricsWindowStatus.IsMaximized ? 0 : 1;
                ExitMaximizeFontIcon.Opacity = LyricsWindowStatus.IsMaximized ? 1 : 0;
            }
        }

        private void OnAutoShowOrHideWindowChanged()
        {
            this.SetLyricsWindowVisibilityByPlayingStatus(_gsmtcService.CurrentIsPlaying, DispatcherQueue);
        }

        private void OnIsAdaptToEnvironmentChanged()
        {
            _fgWindowWatcher?.Stop();
            if (LyricsWindowStatus.IsAdaptToEnvironment)
            {
                _fgWindowWatcher?.Start();
            }
        }

        private void OnWorkAreaChanged()
        {
            LyricsWindowStatus.UpdateMonitorBounds();
            if (LyricsWindowStatus.IsWorkArea)
            {
                this.UpdateAppBar();
                LyricsWindowStatus.IsLocked = true;
            }
        }

        private void OnTitleBarAreaChanged()
        {
            this.SetTitleBarArea(LyricsWindowStatus.TitleBarArea);
        }

        // ====

        public void SetTitleBarArea(TitleBarArea titleBarArea)
        {
            switch (titleBarArea)
            {
                case TitleBarArea.None:
                    SetTitleBar(PlaceholderGrid);
                    break;
                case TitleBarArea.Top:
                    SetTitleBar(TopCommandGrid);
                    break;
                case TitleBarArea.Whole:
                    SetTitleBar(RootGrid);
                    break;
                default:
                    break;
            }
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            if (_settingsService.AppSettings.GeneralSettings.ExitOnLyricsWindowClosed)
            {
                WindowHook.ExitApp();
            }
            else
            {
                this.PrepareWindowClosing();
            }
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            this.Closed -= Window_Closed;

            WeakReferenceMessenger.Default.UnregisterAll(this);

            StopOverlayInputHelper();

            AppWindow.Changed -= AppWindow_Changed;
            AppWindow.Closing -= AppWindow_Closing;

            _wmm?.WindowMessageReceived -= Wmm_WindowMessageReceived;
            _wmm?.Dispose();
            _wmm = null;

            _fgWindowWatcherTimer?.Stop();
            _fgWindowWatcherTimer = null;

            _fgWindowWatcher?.Stop();
            _fgWindowWatcher = null;

            _taskbarHook?.Dispose();
            _taskbarHook = null;
        }

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (args.DidPositionChange || args.DidSizeChange)
            {
                if (AppWindow == null) return;

                var size = AppWindow.Size;
                var rect = AppWindow.Position;

                if (rect.X < 0 && rect.Y < 0 && rect.X + size.Width < 0 && rect.Y + size.Height < 0)
                {
                    return;
                }
                else if (!LyricsWindowStatus.IsWallpaper && (LyricsWindowStatus.IsMaximized || LyricsWindowStatus.IsFullscreen))
                {
                    return;
                }
                else
                {
                    LyricsWindowStatus.WindowBounds = new Rect(rect.X, rect.Y, size.Width, size.Height);
                }
            }
        }

        private void TopCommandGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            TopCommandGrid.Opacity = 1f;
        }

        private void TopCommandGrid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            TopCommandGrid.Opacity = 0f;
        }

        private void MusicGalleryButton_Click(object sender, RoutedEventArgs e)
        {
            WindowHook.OpenOrShowWindow<MusicGalleryWindow>();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsService.AppSettings.GeneralSettings.ExitOnLyricsWindowClosed)
            {
                WindowHook.ExitApp();
            }
            else
            {
                this.CloseWindow();
            }
        }

        private void LyricsWindowSwitchButton_Click(object sender, RoutedEventArgs e)
        {
            WindowHook.OpenOrShowWindow<LyricsWindowSwitchWindow>();
        }

        private void SettingsWindowButton_Click(object sender, RoutedEventArgs e)
        {
            WindowHook.OpenOrShowWindow<SettingsWindow>();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.MinimizeWindow();
        }

        private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateNowPlayingBarStatus();
            UpdateTopCommandGridStatus();
        }

        private void UpdateNowPlayingBarStatus()
        {
            NowPlayingBar.IsCompactMode = LyricsWindowStatus.IsAlwaysHidePlayingBar || RootGrid.ActualWidth < 180 || RootGrid.ActualHeight <= 72;

            NowPlayingBar.ShowTime = NowPlayingBar.ShowVolumeButton = NowPlayingBar.ShowMoreButton =
                NowPlayingBar.IsCompactMode || RootGrid.ActualWidth > 350;
        }

        private void UpdateTopCommandGridStatus()
        {
            if (RootGrid.ActualWidth < 400)
            {
                TopCenterCommandGrid.Visibility = Visibility.Visible;
                if (TopCommandGrid.Children.Contains(TopLeftCommandGrid))
                {
                    TopCommandGrid.Children.Remove(TopLeftCommandGrid);
                }
                if (TopCommandGrid.Children.Contains(TopRightCommandGrid))
                {
                    TopCommandGrid.Children.Remove(TopRightCommandGrid);
                }
                if (!TopCommandFlyoutContainer.Children.Contains(TopLeftCommandGrid))
                {
                    TopCommandFlyoutContainer.Children.Add(TopLeftCommandGrid);
                }
                if (!TopCommandFlyoutContainer.Children.Contains(TopRightCommandGrid))
                {
                    TopCommandFlyoutContainer.Children.Add(TopRightCommandGrid);
                }
            }
            else
            {
                TopCenterCommandGrid.Visibility = Visibility.Collapsed;
                TopCommandFlyoutContainer.Children.Clear();
                if (!TopCommandGrid.Children.Contains(TopLeftCommandGrid))
                {
                    TopCommandGrid.Children.Add(TopLeftCommandGrid);
                }
                if (!TopCommandGrid.Children.Contains(TopRightCommandGrid))
                {
                    TopCommandGrid.Children.Add(TopRightCommandGrid);
                }
            }
        }

        private void StartOverlayInputHelper()
        {
            if (_overlayInputHelper != null) return;

            _overlayInputHelper = new(this);
            _overlayInputHelper.Register(RootGrid);
            _overlayInputHelper.Register(LockToggleButtonContainer);
            if (LyricsWindowStatus.KeepNowPlayingBarInteractiveWhenLocked)
            {
                _overlayInputHelper.Register(NowPlayingBar);
            }
            _overlayInputHelper.OnInteractiveAreaMoved = (args) =>
            {
                if (args.Elements.Contains(LockToggleButtonContainer) || args.Elements.Contains(NowPlayingBar))
                {
                    this.SetIsClickThrough(false);
                }
                else
                {
                    UnlockButton.Opacity = 1;
                    this.SetIsClickThrough(true);
                }
            };
            _overlayInputHelper.OnInteractiveAreaExited = () =>
            {
                UnlockButton.Opacity = 0;
            };
            _overlayInputHelper.Start();
        }

        public void StopOverlayInputHelper()
        {
            _overlayInputHelper?.Stop();
            _overlayInputHelper = null;
        }

        public void RestartOverlayInputHelper()
        {
            StopOverlayInputHelper();
            StartOverlayInputHelper();
        }

        private void UnlockButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (LyricsWindowStatus.IsLocked)
            {
                UnlockButton.Opacity = 1;
            }
        }

        private void UnlockButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (LyricsWindowStatus.IsLocked)
            {
                UnlockButton.Opacity = 0;
            }
        }

        private void UnlockButton_Click(object sender, RoutedEventArgs e)
        {
            LyricsWindowStatus.IsLocked = false;
        }

        private void LockButton_Click(object sender, RoutedEventArgs e)
        {
            LyricsWindowStatus.IsLocked = true;
        }

        private void AOTButton_Click(object sender, RoutedEventArgs e)
        {
            LyricsWindowStatus.IsAlwaysOnTop = !LyricsWindowStatus.IsAlwaysOnTop;
        }

        private void FullscreenButton_Click(object sender, RoutedEventArgs e)
        {
            LyricsWindowStatus.IsFullscreen = !LyricsWindowStatus.IsFullscreen;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            LyricsWindowStatus.IsMaximized = !LyricsWindowStatus.IsMaximized;
        }

        public void Receive(PropertyChangedMessage<bool> message)
        {
            if (message.Sender is IGSMTCService)
            {
                if (message.PropertyName == nameof(IGSMTCService.CurrentIsPlaying))
                {
                    OnAutoShowOrHideWindowChanged();
                }
            }
            else if (message.Sender == LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.IsWorkArea))
                {
                    OnIsWorkAreaChanged();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.IsShownInSwitchers))
                {
                    OnIsShownInSwitchersChanged();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.IsAlwaysOnTop))
                {
                    OnIsAlwaysOnTopChanged();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.IsLocked))
                {
                    OnIsLockedChanged();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.IsFullscreen))
                {
                    OnIsFullscreenChanged();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.IsMaximized))
                {
                    OnIsMaximizedChanged();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.AutoShowOrHideWindow))
                {
                    OnAutoShowOrHideWindowChanged();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.IsAdaptToEnvironment))
                {
                    OnIsAdaptToEnvironmentChanged();
                    _ = UpdateAlbumArtThemeColorsAsync();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.IsPinToTaskbar))
                {
                    OnIsPinToTaskbarChanged();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.IsAlwaysHideUnlockButton))
                {
                    OnIsAlwaysHideUnlockButtonChanged();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.KeepNowPlayingBarInteractiveWhenLocked))
                {
                    if (LyricsWindowStatus.IsLocked)
                    {
                        if (!LyricsWindowStatus.IsWallpaper)
                        {
                            RestartOverlayInputHelper();
                        }
                    }
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.IsAlwaysHidePlayingBar))
                {
                    UpdateNowPlayingBarStatus();
                }
            }
        }

        public void Receive(PropertyChangedMessage<BitmapImage?> message)
        {
            if (message.Sender is IGSMTCService)
            {
                if (message.PropertyName == nameof(IGSMTCService.AlbumArtBitmapImage))
                {
                    _ = UpdateAlbumArtThemeColorsAsync();
                }
            }
        }

        public void Receive(PropertyChangedMessage<double> message)
        {
            if (message.Sender == LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.DockHeight))
                {
                    OnWorkAreaChanged();
                }
            }
        }

        public void Receive(PropertyChangedMessage<string> message)
        {
            if (message.Sender == LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.MonitorDeviceName))
                {
                    OnWorkAreaChanged();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.Name))
                {
                    this.Title = $"{LyricsWindowStatus.Name} - {Constants.App.AppName}";
                }
            }
        }

        public void Receive(PropertyChangedMessage<DockPlacement> message)
        {
            if (message.Sender == LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.DockPlacement))
                {
                    OnWorkAreaChanged();
                }
            }
        }

        public void Receive(PropertyChangedMessage<TitleBarArea> message)
        {
            if (message.Sender == LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.TitleBarArea))
                {
                    OnTitleBarAreaChanged();
                }
            }
        }

        public void Receive(PropertyChangedMessage<ElementTheme> message)
        {
            if (message.Sender == LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.WindowTheme))
                {
                    _ = UpdateAlbumArtThemeColorsAsync();
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsFontColorType> message)
        {
            if (message.Sender == LyricsWindowStatus.LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsBgFontColorType))
                {
                    _ = UpdateAlbumArtThemeColorsAsync();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsPlayedFgFontColorType))
                {
                    _ = UpdateAlbumArtThemeColorsAsync();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsUnplayedFgFontColorType))
                {
                    _ = UpdateAlbumArtThemeColorsAsync();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsPlayedStrokeFontColorType))
                {
                    _ = UpdateAlbumArtThemeColorsAsync();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsUnplayedStrokeFontColorType))
                {
                    _ = UpdateAlbumArtThemeColorsAsync();
                }
            }
            else if (message.Sender == LyricsWindowStatus.LyricsBackgroundSettings)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.LyricsBackgroundSettings.SpectrumColorType))
                {
                    _ = UpdateAlbumArtThemeColorsAsync();
                }
            }
        }

        public void Receive(PropertyChangedMessage<Color> message)
        {
            if (message.Sender == LyricsWindowStatus.LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsCustomBgFontColor))
                {
                    _ = UpdateAlbumArtThemeColorsAsync();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsCustomPlayedFgFontColor))
                {
                    _ = UpdateAlbumArtThemeColorsAsync();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsCustomUnplayedFgFontColor))
                {
                    _ = UpdateAlbumArtThemeColorsAsync();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsCustomPlayedStrokeFontColor))
                {
                    _ = UpdateAlbumArtThemeColorsAsync();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsCustomUnplayedStrokeFontColor))
                {
                    _ = UpdateAlbumArtThemeColorsAsync();
                }
            }
            else if (message.Sender == LyricsWindowStatus.LyricsBackgroundSettings)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.LyricsBackgroundSettings.SpectrumCustomColor))
                {
                    _ = UpdateAlbumArtThemeColorsAsync();
                }
            }
        }

        public void Receive(PropertyChangedMessage<TaskbarPlacement> message)
        {
            if (message.Sender == LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.TaskbarPlacement))
                {
                    _taskbarHook?.UpdatePlacement(LyricsWindowStatus.TaskbarPlacement);
                }
            }
        }

        public void Receive(PropertyChangedMessage<PaletteGeneratorType> message)
        {
            if (message.Sender == LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.PaletteGeneratorType))
                {
                    _ = UpdateAlbumArtThemeColorsAsync();
                }
            }
        }

    }
}
