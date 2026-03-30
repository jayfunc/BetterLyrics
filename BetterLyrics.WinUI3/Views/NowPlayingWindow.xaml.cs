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
using WinUIEx;
using WinUIEx.Messaging;
using static Vanara.PInvoke.User32;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class NowPlayingWindow : Window,
        IRecipient<PropertyChangedMessage<bool>>,
        IRecipient<PropertyChangedMessage<double>>,
        IRecipient<PropertyChangedMessage<string>>,
        IRecipient<PropertyChangedMessage<int>>,
        IRecipient<PropertyChangedMessage<DockPlacement>>,
        IRecipient<PropertyChangedMessage<TitleBarArea>>,
        IRecipient<PropertyChangedMessage<ElementTheme>>,
        IRecipient<PropertyChangedMessage<BitmapImage?>>,
        IRecipient<PropertyChangedMessage<LyricsFontColorType>>,
        IRecipient<PropertyChangedMessage<Color>>,
        IRecipient<PropertyChangedMessage<TaskbarPlacement>>,
        IRecipient<PropertyChangedMessage<PaletteGeneratorType>>
    {
        private readonly SimpleTimer _alwaysOnTopPollingTimer;
        private readonly SimpleTimer _underlayColorTimer;
        private readonly DispatcherQueueTimer _visibilityTimer;
        private OverlayInputHelper? _overlayInputHelper;
        private TaskbarHook? _taskbarHook;
        private WindowMessageMonitor? _wmm;

        private Color _backdropAccentColor = Colors.Transparent;

        public LyricsWindowStatus LyricsWindowStatus { get; private set; }

        private readonly IGSMTCService _gsmtcService = Ioc.Default.GetRequiredService<IGSMTCService>();
        private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

        public NowPlayingWindow(LyricsWindowStatus status)
        {
            this.InitializeComponent();
            _wmm = new WindowMessageMonitor(this);
            _wmm.WindowMessageReceived += Wmm_WindowMessageReceived;

            _alwaysOnTopPollingTimer = new(() =>
            {
                if (LyricsWindowStatus?.IsWallpaper != true)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        this.SetIsAlwaysOnTop(true);
                    });
                }
            });

            _underlayColorTimer = new(() =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    UpdateBackdropAccentColor();
                });
            });

            _visibilityTimer = DispatcherQueue.CreateTimer();

            LyricsWindowStatus = status;
            NowPlayingPage.LyricsWindowStatus = LyricsWindowStatus;
            NowPlayingBar.LyricsWindowStatus = LyricsWindowStatus;

            this.Init(title: $"{status.Name} - {Constants.App.AppName}", titleBarHeightOption: TitleBarHeightOption.Collapsed, backdropType: BackdropType.Transparent);

            AppWindow.Closing += AppWindow_Closing;

            WeakReferenceMessenger.Default.RegisterAll(this);

            _ = UpdateAlbumArtThemeColorsAsync();

            LyricsWindowStatus.WindowStatus = WindowStatus.Opened;
            InitStatus();
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
            else if (msg == WindowMessage.WM_SETTINGCHANGE)
            {
                string? changedSetting = Marshal.PtrToStringUni(e.Message.LParam);
                if (changedSetting == "Desktop")
                {
                    if (LyricsWindowStatus.IsWallpaper && LyricsWindowStatus.IsLocked)
                    {
                        DispatcherQueueHelper.Instance?.TryEnqueue(() =>
                        {
                            WorkerWHook.UnpinFromDesktop(this);
                            WorkerWHook.PinToDesktop(this);
                        });
                    }
                }
            }
        }

        private void OnTaskbarFreeBoundsChanged(Events.TaskbarFreeBoundsChangedEventArgs obj)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                this.MoveAndResize(obj.TaskbarFreeBounds);
            });
        }

        private void InitStatus()
        {
            OnIsShownInSwitchersChanged();
            OnIsAlwaysOnTopChanged();
            OnTitleBarAreaChanged();
            OnIsAdaptToEnvironmentChanged();

            if (LyricsWindowStatus.IsWallpaper)
            {
                AppWindow.Changed += AppWindow_Changed;
            }

            OnIsLockedChanged();

            if (LyricsWindowStatus.IsPinToTaskbar)
            {
                OnIsPinToTaskbarChanged();
                AppWindow.Changed += AppWindow_Changed;
            }
            else if (LyricsWindowStatus.IsWallpaper)
            {
            }
            else if (LyricsWindowStatus.IsWorkArea)
            {
                OnIsWorkAreaChanged();
                AppWindow.Changed += AppWindow_Changed;
            }
            else
            {
                this.MoveAndResize(LyricsWindowStatus.WindowBounds);
                AppWindow.Changed += AppWindow_Changed;
                if (LyricsWindowStatus.IsMaximized)
                {
                    MaximizeButton_Click(null, null);
                }
                if (LyricsWindowStatus.IsFullscreen)
                {
                    FullscreenButton_Click(null, null);
                }
            }

            LyricsWindowStatus.UpdateMonitorBounds();
            OnAutoShowOrHideWindowChanged();
        }

        public void UpdateBackdropAccentColor()
        {
            var oldValue = _backdropAccentColor;
            var newValue = Helper.ColorHelper.GetAccentColor(
                WindowNative.GetWindowHandle(this),
                LyricsWindowStatus.MonitorDeviceName,
                LyricsWindowStatus.EnvironmentSampleMode);
            // 防止不必要刷新导致界面不流畅
            if (newValue != oldValue)
            {
                _backdropAccentColor = newValue;
                _ = UpdateAlbumArtThemeColorsAsync();
            }
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
                UpdateBackdropAccentColor();
            }
            else
            {
                // 强制触发一次更新，刷新解锁图标可见性状态
                OnIsLockedChanged();
            }
        }

        private void OnIsShownInSwitchersChanged()
        {
            this.AppWindow.IsShownInSwitchers = LyricsWindowStatus.IsShownInSwitchers;
        }

        private void OnIsAlwaysOnTopChanged()
        {
            this.SetIsAlwaysOnTop(LyricsWindowStatus.IsAlwaysOnTop);
            PinFillFontIcon.Opacity = LyricsWindowStatus.IsAlwaysOnTop ? 1 : 0;
            OnIsAlwaysOnTopPollingChanged();
        }

        private void OnIsAlwaysOnTopPollingChanged()
        {
            _alwaysOnTopPollingTimer.Stop();
            LyricsWindowStatus.IsAlwaysOnTopPollingTimerRunning = false;

            if (LyricsWindowStatus.IsAlwaysOnTop && LyricsWindowStatus.IsAlwaysOnTopPolling)
            {
                _alwaysOnTopPollingTimer.Start();
                LyricsWindowStatus.IsAlwaysOnTopPollingTimerRunning = true;
            }
        }

        private void OnIsLockedChanged()
        {
            UnlockButton.Visibility = LyricsWindowStatus.IsAlwaysHideUnlockButton ? Visibility.Collapsed : Visibility.Visible;
            StopOverlayInputHelper();

            if (LyricsWindowStatus.IsLocked)
            {
                LockToggleButtonContainer.Visibility = Visibility.Visible;
                if (LyricsWindowStatus.IsWallpaper)
                {
                    WorkerWHook.PinToDesktop(this);
                }
                else if (!LyricsWindowStatus.IsAlwaysHideUnlockButton || LyricsWindowStatus.KeepNowPlayingBarInteractiveWhenLocked)
                {
                    StartOverlayInputHelper();
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

        private void OnAutoShowOrHideWindowChanged()
        {
            var status = LyricsWindowStatus;

            if (status.AutoShowOrHideWindow)
            {
                _visibilityTimer.Debounce(() =>
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (_gsmtcService.CurrentIsPlaying && status.WindowStatus == WindowStatus.HiddenBySystem)
                        {
                            WindowHook.OpenOrShowWindow<NowPlayingWindow>(status);
                            if (status.IsWorkArea)
                            {
                                this.SetIsWorkArea(true);
                                this.MoveAndResize(status.GetWindowBoundsWhenWorkArea());
                            }
                            if (status.IsLocked && status.IsWallpaper && (!status.IsAlwaysHideUnlockButton || status.KeepNowPlayingBarInteractiveWhenLocked))
                            {
                                RestartOverlayInputHelper();
                            }
                        }
                        else if (!_gsmtcService.CurrentIsPlaying && status.WindowStatus == WindowStatus.Opened)
                        {
                            this.HideWindow(WindowStatus.HiddenBySystem);
                            StopOverlayInputHelper();
                        }
                    });
                }, TimeSpan.FromMilliseconds(LyricsWindowStatus.AutoShowOrHideWindowDelay));
            }
        }

        private void OnIsAdaptToEnvironmentChanged()
        {
            _underlayColorTimer.Stop();
            LyricsWindowStatus.IsUnderlayColorTimerRunning = false;

            if (LyricsWindowStatus.IsAdaptToEnvironment)
            {
                _underlayColorTimer.Start();
                LyricsWindowStatus.IsUnderlayColorTimerRunning = true;
            }
            else
            {
                _backdropAccentColor = Colors.Transparent;
                _ = UpdateAlbumArtThemeColorsAsync();
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
            SetTitleBarArea(LyricsWindowStatus.TitleBarArea);
        }

        // ====

        public void SetTitleBarArea(TitleBarArea titleBarArea)
        {
            if (AppWindow == null) return;

            double scale = RootGrid.XamlRoot?.RasterizationScale ?? 1.0;

            switch (titleBarArea)
            {
                case TitleBarArea.None:
                    AppWindow.TitleBar.SetDragRectangles([new Windows.Graphics.RectInt32(0, 0, 0, 0)]);
                    break;

                case TitleBarArea.Top:
                    AppWindow.TitleBar.SetDragRectangles([
                        new Windows.Graphics.RectInt32(
                            0,
                            0,
                            (int)(TopCommandGrid.ActualWidth * scale),
                            (int)(TopCommandGrid.ActualHeight * scale)
                        )
                    ]);
                    break;

                case TitleBarArea.Whole:
                    AppWindow.TitleBar.SetDragRectangles([
                        new Windows.Graphics.RectInt32(
                            0,
                            0,
                            (int)(RootGrid.ActualWidth * scale),
                            (int)(RootGrid.ActualHeight * scale)
                        )
                    ]);
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

            RootGrid.XamlRoot?.Changed -= XamlRoot_Changed;

            AppWindow.Changed -= AppWindow_Changed;
            AppWindow.Closing -= AppWindow_Closing;

            _wmm?.WindowMessageReceived -= Wmm_WindowMessageReceived;
            _wmm?.Dispose();
            _wmm = null;

            _alwaysOnTopPollingTimer.Stop();
            _alwaysOnTopPollingTimer.Dispose();
            LyricsWindowStatus.IsAlwaysOnTopPollingTimerRunning = false;

            _underlayColorTimer.Stop();
            _underlayColorTimer.Dispose();
            LyricsWindowStatus.IsUnderlayColorTimerRunning = false;

            _taskbarHook?.Dispose();
            _taskbarHook = null;
        }

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (args.DidPositionChange || args.DidSizeChange || args.DidPresenterChange)
            {
                if (AppWindow == null) return;

                var presenter = AppWindow.Presenter;

                if (presenter is OverlappedPresenter overlappedPresenter)
                {
                    if (overlappedPresenter.State == OverlappedPresenterState.Restored)
                    {
                        EnterMaximizeFontIcon.Opacity = 1;
                        ExitMaximizeFontIcon.Opacity = 0;
                        LyricsWindowStatus.IsMaximized = false;
                    }
                    else if (overlappedPresenter.State == OverlappedPresenterState.Maximized)
                    {
                        EnterMaximizeFontIcon.Opacity = 0;
                        ExitMaximizeFontIcon.Opacity = 1;
                        LyricsWindowStatus.IsMaximized = true;
                    }

                    EnterFullscreenFontIcon.Opacity = 1;
                    ExitFullscreenFontIcon.Opacity = 0;
                    MaximizeButton.Visibility = Visibility.Visible;
                    AOTButton.Visibility = Visibility.Visible;
                    MinimizeButton.Visibility = Visibility.Visible;
                    LockButton.Visibility = Visibility.Visible;

                    LyricsWindowStatus.IsFullscreen = false;
                }
                else if (presenter is FullScreenPresenter fullScreenPresenter)
                {
                    EnterMaximizeFontIcon.Opacity = 0;
                    ExitMaximizeFontIcon.Opacity = 0;

                    EnterFullscreenFontIcon.Opacity = 0;
                    ExitFullscreenFontIcon.Opacity = 1;
                    MaximizeButton.Visibility = Visibility.Collapsed;
                    AOTButton.Visibility = Visibility.Collapsed;
                    MinimizeButton.Visibility = Visibility.Collapsed;
                    LockButton.Visibility = Visibility.Collapsed;

                    LyricsWindowStatus.IsMaximized = false;
                    LyricsWindowStatus.IsFullscreen = true;
                }

                if (args.DidPositionChange || args.DidSizeChange)
                {
                    var size = AppWindow.Size;
                    var rect = AppWindow.Position;

                    if (rect.X < 0 && rect.Y < 0 && rect.X + size.Width < 0 && rect.Y + size.Height < 0)
                    {
                        return;
                    }
                    // 仅非壁纸模式才忽略最大化全屏化
                    // 壁纸模式将记忆最大化全屏化之后的坐标以便正确固定到桌面
                    else if (!LyricsWindowStatus.IsWallpaper && (LyricsWindowStatus.IsMaximized || LyricsWindowStatus.IsFullscreen))
                    {
                        return;
                    }
                    // 忽略壁纸模式+已锁定状态防止在固定到桌面的过程中由于坐标系变换导致的错误的坐标被记忆
                    else if (LyricsWindowStatus.IsWallpaper && LyricsWindowStatus.IsLocked)
                    {
                        return;
                    }
                    else
                    {
                        LyricsWindowStatus.WindowBounds = new Rect(rect.X, rect.Y, size.Width, size.Height);
                    }
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
            OnTitleBarAreaChanged();
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
            LyricsWindowStatus.IsOverlayInputHelperRunning = true;
        }

        public void StopOverlayInputHelper()
        {
            _overlayInputHelper?.Stop();
            _overlayInputHelper = null;
            LyricsWindowStatus.IsOverlayInputHelperRunning = false;
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
            if (EnterFullscreenFontIcon.Opacity == 1)
            {
                AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
            }
            else if (ExitFullscreenFontIcon.Opacity == 1)
            {
                AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
            }
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (EnterMaximizeFontIcon.Opacity == 1)
            {
                this.Maximize();
            }
            else if (ExitMaximizeFontIcon.Opacity == 1)
            {
                this.Restore();
            }
        }

        private void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            RootGrid.Margin = new(
                (int)((LyricsWindowStatus.PaddingLeft / 100.0) * (RootGrid.ActualWidth / 2)),
                (int)((LyricsWindowStatus.PaddingTop / 100.0) * (RootGrid.ActualHeight / 2)),
                (int)((LyricsWindowStatus.PaddingRight / 100.0) * (RootGrid.ActualWidth / 2)),
                (int)((LyricsWindowStatus.PaddingBottom / 100.0) * (RootGrid.ActualHeight / 2))
            );
            RootGrid.XamlRoot?.Changed += XamlRoot_Changed;
            OnTitleBarAreaChanged();
        }

        private void XamlRoot_Changed(XamlRoot sender, XamlRootChangedEventArgs args)
        {
            OnTitleBarAreaChanged();
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
                else if (message.PropertyName == nameof(LyricsWindowStatus.IsAlwaysOnTopPolling))
                {
                    OnIsAlwaysOnTopPollingChanged();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.IsLocked))
                {
                    OnIsLockedChanged();
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
                    OnIsLockedChanged();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.KeepNowPlayingBarInteractiveWhenLocked))
                {
                    OnIsLockedChanged();
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

        public void Receive(PropertyChangedMessage<int> message)
        {
            if (message.Sender == LyricsWindowStatus)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.PaddingLeft))
                {
                    RootGrid.Margin = RootGrid.Margin.WithLeft((int)((message.NewValue / 100.0) * (RootGrid.ActualWidth / 2)));
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.PaddingTop))
                {
                    RootGrid.Margin = RootGrid.Margin.WithTop((int)((message.NewValue / 100.0) * (RootGrid.ActualHeight / 2)));
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.PaddingRight))
                {
                    RootGrid.Margin = RootGrid.Margin.WithRight((int)((message.NewValue / 100.0) * (RootGrid.ActualWidth / 2)));
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.PaddingBottom))
                {
                    RootGrid.Margin = RootGrid.Margin.WithBottom((int)((message.NewValue / 100.0) * (RootGrid.ActualHeight / 2)));
                }
            }
        }

    }
}
