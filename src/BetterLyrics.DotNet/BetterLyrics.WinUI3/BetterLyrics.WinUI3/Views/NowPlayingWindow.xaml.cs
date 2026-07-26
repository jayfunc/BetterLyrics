// 2025/6/23 by Zhe Fang

using System.Runtime.InteropServices;
using Windows.Graphics;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helpers;
using BetterLyrics.WinUI3.Hooks;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Vanara.PInvoke;
using WinRT.Interop;
using WinUIEx;
using WinUIEx.Messaging;
using static Vanara.PInvoke.User32;
using Message = BetterLyrics.WinUI3.Constants.Message;
using BetterLyrics.WinUI3.Providers;

namespace BetterLyrics.WinUI3.Views;

public sealed partial class NowPlayingWindow : Window,
    IRecipient<PropertyChangedMessage<bool>>,
    IRecipient<PropertyChangedMessage<double>>,
    IRecipient<PropertyChangedMessage<string>>,
    IRecipient<PropertyChangedMessage<DockPlacement>>,
    IRecipient<PropertyChangedMessage<TitleBarArea>>,
    IRecipient<PropertyChangedMessage<AppTheme>>,
    IRecipient<PropertyChangedMessage<byte[]?>>,
    IRecipient<PropertyChangedMessage<LyricsFontColorType>>,
    IRecipient<PropertyChangedMessage<AppColor>>,
    IRecipient<PropertyChangedMessage<TaskbarPlacement>>,
    IRecipient<PropertyChangedMessage<PaletteGeneratorType>>,
    IRecipient<PropertyChangedMessage<MediaSourceProviderInfo?>>
{
    private readonly Debouncer _albumArtThemeColorsDebounder = new();
    private readonly AsyncPoller _alwaysOnTopPoller = new();

    private readonly IAppUIThreadProvider _appUIThreadProvider =
        Ioc.Default.GetRequiredService<IAppUIThreadProvider>();

    private readonly IGsmtcService _gsmtcService = Ioc.Default.GetRequiredService<IGsmtcService>();

    private readonly ILogger<NowPlayingWindow>
        _logger = Ioc.Default.GetRequiredService<ILogger<NowPlayingWindow>>();

    private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
    private readonly AsyncPoller _underlayColorPoller = new();
    private readonly Debouncer _visibilityDebouncer = new();

    private readonly IWindowManagerProvider _windowManagerProvider =
        Ioc.Default.GetRequiredService<IWindowManagerProvider>();

    private readonly IMonitorProvider _monitorProvider =
        Ioc.Default.GetRequiredService<IMonitorProvider>();

    private AppColor _backdropAccentColor = Colors.Transparent;
    private OverlayInputHelper? _overlayInputHelper;
    private TaskbarHook? _taskbarHook;
    private WindowMessageMonitor? _wmm;

    public NowPlayingWindow(LyricsWindowStatus status)
    {
        InitializeComponent();
        _wmm = new WindowMessageMonitor(this);
        _wmm.WindowMessageReceived += Wmm_WindowMessageReceived;

        LyricsWindowStatus = status;
        NowPlayingPage.LyricsWindowStatus = LyricsWindowStatus;
        NowPlayingBar.LyricsWindowStatus = LyricsWindowStatus;

        this.Init(title: status.Name, titleBarHeightOption: TitleBarHeightOption.Collapsed,
            backdropType: BackdropType.Transparent);

        AppWindow.Closing += AppWindow_Closing;

        WeakReferenceMessenger.Default.RegisterAll(this);

        RequestUpdateAlbumArtThemeColors();

        LyricsWindowStatus.WindowStatus = WindowStatus.Opened;
    }

    public LyricsWindowStatus LyricsWindowStatus { get; }

    public void Receive(PropertyChangedMessage<AppColor> message)
    {
        if (message.Sender == LyricsWindowStatus.LyricsStyleSettings)
        {
            if (message.PropertyName == nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsCustomBgFontColor))
                RequestUpdateAlbumArtThemeColors();
            else if (message.PropertyName ==
                     nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsCustomPlayedFgFontColor))
                RequestUpdateAlbumArtThemeColors();
            else if (message.PropertyName ==
                     nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsCustomUnplayedFgFontColor))
                RequestUpdateAlbumArtThemeColors();
            else if (message.PropertyName ==
                     nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsCustomPlayedStrokeFontColor))
                RequestUpdateAlbumArtThemeColors();
            else if (message.PropertyName ==
                     nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsCustomUnplayedStrokeFontColor))
                RequestUpdateAlbumArtThemeColors();
        }
        else if (message.Sender == LyricsWindowStatus.LyricsBackgroundSettings)
        {
            if (message.PropertyName == nameof(LyricsWindowStatus.LyricsBackgroundSettings.SpectrumCustomColor))
                RequestUpdateAlbumArtThemeColors();
        }
    }

    public void Receive(PropertyChangedMessage<AppTheme> message)
    {
        if (message.Sender == LyricsWindowStatus)
            if (message.PropertyName == nameof(LyricsWindowStatus.WindowTheme))
                RequestUpdateAlbumArtThemeColors();
    }

    public void Receive(PropertyChangedMessage<bool> message)
    {
        if (message.Sender is IGsmtcService)
        {
            if (message.PropertyName == nameof(IGsmtcService.CurrentIsPlaying)) OnAutoShowOrHideWindowChanged();
        }
        else if (message.Sender == LyricsWindowStatus)
        {
            if (message.PropertyName == nameof(LyricsWindowStatus.IsShownInSwitchers))
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
            else if (message.PropertyName == nameof(LyricsWindowStatus.HideWindowWhenPaused))
            {
                OnAutoShowOrHideWindowChanged();
            }
            else if (message.PropertyName == nameof(LyricsWindowStatus.HideWindowWhenNullSession))
            {
                OnAutoShowOrHideWindowChanged();
            }
            else if (message.PropertyName == nameof(LyricsWindowStatus.IsAdaptToEnvironment))
            {
                OnIsAdaptToEnvironmentChanged();
                RequestUpdateAlbumArtThemeColors();
            }
            else if (message.PropertyName == nameof(LyricsWindowStatus.IsAdaptToAlbumArt))
            {
                RequestUpdateAlbumArtThemeColors();
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

    public void Receive(PropertyChangedMessage<byte[]?> message)
    {
        if (message.Sender is IGsmtcService)
            if (message.PropertyName == nameof(IGsmtcService.AlbumArtBytes))
                RequestUpdateAlbumArtThemeColors();
    }

    public void Receive(PropertyChangedMessage<DockPlacement> message)
    {
        if (message.Sender == LyricsWindowStatus)
            if (message.PropertyName == nameof(LyricsWindowStatus.DockPlacement))
                OnWorkAreaChanged();
    }

    public void Receive(PropertyChangedMessage<double> message)
    {
        if (message.Sender == LyricsWindowStatus)
        {
            if (message.PropertyName == nameof(LyricsWindowStatus.DockHeight))
                OnWorkAreaChanged();
            else if (message.PropertyName == nameof(LyricsWindowStatus.PaletteChromaWeight) ||
                     message.PropertyName == nameof(LyricsWindowStatus.PaletteToneWeight) ||
                     message.PropertyName == nameof(LyricsWindowStatus.PalettePopulationWeight))
                RequestUpdateAlbumArtThemeColors();
        }
    }

    public void Receive(PropertyChangedMessage<LyricsFontColorType> message)
    {
        if (message.Sender == LyricsWindowStatus.LyricsStyleSettings)
        {
            if (message.PropertyName == nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsBgFontColorType))
                RequestUpdateAlbumArtThemeColors();
            else if (message.PropertyName ==
                     nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsPlayedFgFontColorType))
                RequestUpdateAlbumArtThemeColors();
            else if (message.PropertyName ==
                     nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsUnplayedFgFontColorType))
                RequestUpdateAlbumArtThemeColors();
            else if (message.PropertyName ==
                     nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsPlayedStrokeFontColorType))
                RequestUpdateAlbumArtThemeColors();
            else if (message.PropertyName ==
                     nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsUnplayedStrokeFontColorType))
                RequestUpdateAlbumArtThemeColors();
        }
        else if (message.Sender == LyricsWindowStatus.LyricsBackgroundSettings)
        {
            if (message.PropertyName == nameof(LyricsWindowStatus.LyricsBackgroundSettings.SpectrumColorType))
                RequestUpdateAlbumArtThemeColors();
        }
    }

    public void Receive(PropertyChangedMessage<MediaSourceProviderInfo?> message)
    {
        if (message.Sender is IGsmtcService)
            if (message.PropertyName == nameof(IGsmtcService.CurrentMediaSourceProviderInfo))
                OnAutoShowOrHideWindowChanged();
    }

    public void Receive(PropertyChangedMessage<PaletteGeneratorType> message)
    {
        if (message.Sender == LyricsWindowStatus)
            if (message.PropertyName == nameof(LyricsWindowStatus.PaletteGeneratorType))
                RequestUpdateAlbumArtThemeColors();
    }

    public void Receive(PropertyChangedMessage<string> message)
    {
        if (message.Sender == LyricsWindowStatus)
        {
            if (message.PropertyName == nameof(LyricsWindowStatus.MonitorDeviceName))
                OnWorkAreaChanged();
            else if (message.PropertyName == nameof(LyricsWindowStatus.Name))
                Title = $"{LyricsWindowStatus.Name} - {Core.Constants.App.AppName}";
        }
    }

    public void Receive(PropertyChangedMessage<TaskbarPlacement> message)
    {
        if (message.Sender == LyricsWindowStatus)
            if (message.PropertyName == nameof(LyricsWindowStatus.TaskbarPlacement))
                _taskbarHook?.UpdatePlacement(LyricsWindowStatus.TaskbarPlacement);
    }

    public void Receive(PropertyChangedMessage<TitleBarArea> message)
    {
        if (message.Sender == LyricsWindowStatus)
            if (message.PropertyName == nameof(LyricsWindowStatus.TitleBarArea))
                OnTitleBarAreaChanged();
    }

    private void Wmm_WindowMessageReceived(object? sender, WindowMessageEventArgs e)
    {
        var msgId = e.Message.MessageId;
        if (msgId == Message.WM_APPBAR_CALLBACK)
        {
            var notification = (Shell32.ABN)e.Message.WParam;

            switch (notification)
            {
                case Shell32.ABN.ABN_POSCHANGED:
                    // 位置发生变化
                    _windowManagerProvider.MoveAndResize(this, LyricsWindowStatus.GetAppBarBounds());
                    break;

                case Shell32.ABN.ABN_STATECHANGE:
                    // 状态（自动隐藏/置顶）发生了改变
                    _windowManagerProvider.MoveAndResize(this, LyricsWindowStatus.GetAppBarBounds());
                    break;

                case Shell32.ABN.ABN_FULLSCREENAPP:
                    // 有其他窗口进入或退出了全屏状态
                    // e.Message.LParam == 1 代表有窗口全屏
                    _windowManagerProvider.MoveAndResize(this, LyricsWindowStatus.GetAppBarBounds());
                    break;
            }

            e.Handled = true;
        }
        else
        {
            var msg = (WindowMessage)msgId;
            if (msg == WindowMessage.WM_SETTINGCHANGE)
            {
                var changedSetting = Marshal.PtrToStringUni(e.Message.LParam);
                if (changedSetting == "Desktop")
                    if (LyricsWindowStatus.IsWallpaper && LyricsWindowStatus.IsLocked)
                        _appUIThreadProvider.Execute(() =>
                        {
                            WorkerWHook.UnpinFromDesktop(this);
                            WorkerWHook.PinToDesktop(this);
                        });
            }
        }
    }

    private void InitStatus()
    {
        OnIsShownInSwitchersChanged();
        OnIsAlwaysOnTopChanged();
        OnTitleBarAreaChanged();
        OnIsAdaptToEnvironmentChanged();

        if (LyricsWindowStatus.IsPinToTaskbar)
        {
            AppWindow.Changed += AppWindow_Changed;
            _windowManagerProvider.MoveAndResize(this, LyricsWindowStatus.WindowBounds);
            OnIsLockedChanged();
            Activate();
        }
        else if (LyricsWindowStatus.IsWallpaper)
        {
            AppWindow.Changed += AppWindow_Changed;
            _windowManagerProvider.MoveAndResize(this, LyricsWindowStatus.WindowBounds);
            OnIsLockedChanged();
            Activate();
        }
        else if (LyricsWindowStatus.IsWorkArea)
        {
            _windowManagerProvider.SetIsAppBar(this, true);
            LyricsWindowStatus.IsLocked = true;
            UpdateBackdropAccentColor();
            OnIsLockedChanged();
            AppWindow.Changed += AppWindow_Changed;
            Activate();
        }
        else
        {
            _windowManagerProvider.MoveAndResize(this, LyricsWindowStatus.WindowBounds);
            OnIsLockedChanged();
            AppWindow.Changed += AppWindow_Changed;
            if (LyricsWindowStatus.IsFullscreen)
            {
                Activate();
                this.SetWindowPresenter(AppWindowPresenterKind.FullScreen);
            }
            else if (LyricsWindowStatus.IsMaximized)
            {
                this.Maximize();
                Activate();
            }
            else
            {
                Activate();
            }
        }

        OnAutoShowOrHideWindowChanged();
    }

    public void UpdateBackdropAccentColor()
    {
        var oldValue = _backdropAccentColor;
        var newValue = ColorHelper.GetAccentColor(
            WindowNative.GetWindowHandle(this),
            LyricsWindowStatus.EnvironmentSampleMode);
        // 防止不必要刷新导致界面不流畅
        if (newValue != oldValue)
        {
            _backdropAccentColor = newValue;
            RequestUpdateAlbumArtThemeColors();
        }
    }

    private void RequestUpdateAlbumArtThemeColors()
    {
        _ = _albumArtThemeColorsDebounder.RunAsync(async () =>
        {
            var result =
                await _gsmtcService.CalculateAlbumArtThemeColorsAsync(LyricsWindowStatus, _backdropAccentColor);

            _appUIThreadProvider.Execute(() =>
            {
                NowPlayingPage.LyricsWindowStatus?.WindowPalette = result;
                RootGrid.RequestedTheme = result.ThemeType.ToElementTheme();
            });
        });
    }

    private void UpdateMonitorNameAndBounds()
    {
        var (name, rect) = _monitorProvider.GetMonitorInfoFromWindow(this);
        LyricsWindowStatus.MonitorDeviceName = name;
        LyricsWindowStatus.MonitorBounds = rect;
    }

    // ====

    private void OnIsShownInSwitchersChanged()
    {
        AppWindow.IsShownInSwitchers = LyricsWindowStatus.IsShownInSwitchers;
    }

    private void OnIsAlwaysOnTopChanged()
    {
        this.SetIsAlwaysOnTop(LyricsWindowStatus.IsAlwaysOnTop);
        PinFillFontIcon.Opacity = LyricsWindowStatus.IsAlwaysOnTop ? 1 : 0;
        OnIsAlwaysOnTopPollingChanged();
    }

    private void OnIsAlwaysOnTopPollingChanged()
    {
        _alwaysOnTopPoller.Stop();
        LyricsWindowStatus.IsAlwaysOnTopPollingTimerRunning = false;

        if (LyricsWindowStatus.IsAlwaysOnTop && LyricsWindowStatus.IsAlwaysOnTopPolling)
        {
            _alwaysOnTopPoller.Start(async token =>
            {
                if (LyricsWindowStatus?.IsWallpaper != true)
                    _appUIThreadProvider.Execute(() => { this.SetIsAlwaysOnTop(true); });
            });
            LyricsWindowStatus.IsAlwaysOnTopPollingTimerRunning = true;
        }
    }

    private void OnIsLockedChanged()
    {
        if (LyricsWindowStatus.IsBorderlessWhenLocked)
            _windowManagerProvider.SetIsBorderless(this, LyricsWindowStatus.IsLocked);

        if (!LyricsWindowStatus.IsWallpaper) _windowManagerProvider.SetIsBorderless(this, LyricsWindowStatus.IsLocked);

        UnlockButton.Visibility =
            LyricsWindowStatus.IsAlwaysHideUnlockButton ? Visibility.Collapsed : Visibility.Visible;
        StopOverlayInputHelper();

        if (LyricsWindowStatus.IsLocked)
        {
            LockToggleButtonContainer.Visibility = Visibility.Visible;
            if (LyricsWindowStatus.IsWallpaper)
            {
                WorkerWHook.PinToDesktop(this);
            }
            else
            {
                if (LyricsWindowStatus.IsPinToTaskbar) PinToTaskbar();

                if (!LyricsWindowStatus.IsAlwaysHideUnlockButton ||
                    LyricsWindowStatus.KeepNowPlayingBarInteractiveWhenLocked)
                {
                    StartOverlayInputHelper();
                }
                else
                {
                    _windowManagerProvider.SetIsClickThrough(this, true);
                }
            }
        }
        else
        {
            LockToggleButtonContainer.Visibility = Visibility.Collapsed;
            UnlockButton.Opacity = 0;
            _windowManagerProvider.SetIsClickThrough(this, false);
            if (LyricsWindowStatus.IsWallpaper)
            {
                WorkerWHook.UnpinFromDesktop(this);
            }
            else if (LyricsWindowStatus.IsPinToTaskbar)
            {
                _taskbarHook?.Dispose();
                _taskbarHook = null;
            }
        }
    }

    private void PinToTaskbar()
    {
        _taskbarHook?.Dispose();
        _taskbarHook = null;

        _taskbarHook = new TaskbarHook(this, LyricsWindowStatus.TaskbarPlacement, LyricsWindowStatus.MonitorBounds);
    }

    private void OnAutoShowOrHideWindowChanged()
    {
        var status = LyricsWindowStatus;

        if (status.HideWindowWhenPaused || status.HideWindowWhenNullSession)
            _ = _visibilityDebouncer.RunAsync(() =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (status.WindowStatus == WindowStatus.HiddenBySystem)
                    {
                        if ((status.HideWindowWhenPaused && _gsmtcService.CurrentIsPlaying)
                            || (status.HideWindowWhenNullSession &&
                                _gsmtcService.CurrentMediaSourceProviderInfo != null))
                        {
                            _windowManagerProvider.OpenOrShowWindow<NowPlayingWindow>(status);
                            if (status.IsWorkArea)
                            {
                                _windowManagerProvider.SetIsAppBar(this, true);
                                _windowManagerProvider.MoveAndResize(this, status.GetAppBarBounds());
                            }

                            if (status.IsLocked && status.IsWallpaper && (!status.IsAlwaysHideUnlockButton ||
                                                                          status
                                                                              .KeepNowPlayingBarInteractiveWhenLocked))
                                RestartOverlayInputHelper();
                        }
                    }
                    else if (status.WindowStatus == WindowStatus.Opened)
                    {
                        if ((status.HideWindowWhenPaused && !_gsmtcService.CurrentIsPlaying)
                            || (status.HideWindowWhenNullSession &&
                                _gsmtcService.CurrentMediaSourceProviderInfo == null))
                        {
                            _windowManagerProvider.HideWindow(this, WindowStatus.HiddenBySystem);
                            StopOverlayInputHelper();
                        }
                    }
                });
            }, LyricsWindowStatus.AutoShowOrHideWindowDelay);
    }

    private void OnIsAdaptToEnvironmentChanged()
    {
        _underlayColorPoller.Stop();
        LyricsWindowStatus.IsUnderlayColorTimerRunning = false;

        if (LyricsWindowStatus.IsAdaptToEnvironment)
        {
            _underlayColorPoller.Start(async token =>
            {
                _appUIThreadProvider.Execute(() => { UpdateBackdropAccentColor(); });
            });
            LyricsWindowStatus.IsUnderlayColorTimerRunning = true;
        }
        else
        {
            _backdropAccentColor = Colors.Transparent;
            RequestUpdateAlbumArtThemeColors();
        }
    }

    private void OnWorkAreaChanged()
    {
        UpdateMonitorNameAndBounds();
        if (LyricsWindowStatus.IsWorkArea)
        {
            _windowManagerProvider.UpdateAppBar(this);
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

        var scale = RootGrid.XamlRoot?.RasterizationScale ?? 1.0;

        switch (titleBarArea)
        {
            case TitleBarArea.None:
                AppWindow.TitleBar.SetDragRectangles([new RectInt32(0, 0, 0, 0)]);
                break;

            case TitleBarArea.Top:
                AppWindow.TitleBar.SetDragRectangles([
                    new RectInt32(
                        0,
                        0,
                        (int)(TopCommandGrid.ActualWidth * scale),
                        (int)(TopCommandGrid.ActualHeight * scale)
                    )
                ]);
                break;

            case TitleBarArea.Whole:
                AppWindow.TitleBar.SetDragRectangles([
                    new RectInt32(
                        0,
                        0,
                        (int)(RootGrid.ActualWidth * scale),
                        (int)(RootGrid.ActualHeight * scale)
                    )
                ]);
                break;
        }
    }

    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (_settingsService.AppSettings.GeneralSettings.ExitOnLyricsWindowClosed)
            _windowManagerProvider.ExitApp();
        else
            _windowManagerProvider.PrepareWindowClosing(this);
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        Closed -= Window_Closed;

        WeakReferenceMessenger.Default.UnregisterAll(this);

        StopOverlayInputHelper();

        RootGrid.XamlRoot?.Changed -= XamlRoot_Changed;

        AppWindow?.Changed -= AppWindow_Changed;
        AppWindow?.Closing -= AppWindow_Closing;

        _wmm?.WindowMessageReceived -= Wmm_WindowMessageReceived;
        _wmm?.Dispose();
        _wmm = null;

        _alwaysOnTopPoller.Stop();
        _alwaysOnTopPoller.Dispose();
        LyricsWindowStatus.IsAlwaysOnTopPollingTimerRunning = false;

        _underlayColorPoller.Stop();
        _underlayColorPoller.Dispose();
        LyricsWindowStatus.IsUnderlayColorTimerRunning = false;

        _visibilityDebouncer.Dispose();
        _albumArtThemeColorsDebounder.Dispose();

        _taskbarHook?.Dispose();
        _taskbarHook = null;
    }

    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (args.DidPositionChange || args.DidSizeChange || args.DidPresenterChange)
        {
            if (AppWindow == null) return;

            var presenter = AppWindow.Presenter;

            //Debug.WriteLine(
            //    "AppWindow changed: " +
            //    "PositionChanged={0}, " +
            //    "SizeChanged={1}, " +
            //    "PresenterChanged={2}, " +
            //    "CurrentPresenter={3}, PresenterType={4}",
            //    args.DidPositionChange, args.DidSizeChange, args.DidPresenterChange, presenter?.GetType().Name, presenter?.Kind.ToString());

            if (presenter?.Kind == AppWindowPresenterKind.Overlapped)
            {
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
            }
            else if (presenter?.Kind == AppWindowPresenterKind.FullScreen)
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
                }
                // 仅非壁纸模式才忽略最大化全屏化
                // 壁纸模式将记忆最大化全屏化之后的坐标以便正确固定到桌面
                else if (!LyricsWindowStatus.IsWallpaper &&
                         (LyricsWindowStatus.IsMaximized || LyricsWindowStatus.IsFullscreen))
                {
                }
                // 忽略壁纸模式+已锁定状态防止在固定到桌面的过程中由于坐标系变换导致的错误的坐标被记忆
                else if (LyricsWindowStatus.IsWallpaper && LyricsWindowStatus.IsLocked)
                {
                }
                else
                {
                    LyricsWindowStatus.WindowBounds = new AppRect(rect.X, rect.Y, size.Width, size.Height);
                    UpdateMonitorNameAndBounds();
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
        _windowManagerProvider.OpenOrShowWindow<MusicGalleryWindow>();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_settingsService.AppSettings.GeneralSettings.ExitOnLyricsWindowClosed)
            _windowManagerProvider.ExitApp();
        else
            _windowManagerProvider.CloseWindow(this);
    }

    private void LyricsWindowSwitchButton_Click(object sender, RoutedEventArgs e)
    {
        _windowManagerProvider.OpenOrShowWindow<LyricsWindowSwitchWindow>();
    }

    private void SettingsWindowButton_Click(object sender, RoutedEventArgs e)
    {
        _windowManagerProvider.OpenOrShowWindow<SettingsWindow>();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        _windowManagerProvider.MinimizeWindow(this);
    }

    private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateNowPlayingBarStatus();
        UpdateTopCommandGridStatus();
        OnTitleBarAreaChanged();
    }

    private void UpdateNowPlayingBarStatus()
    {
        NowPlayingBar.IsCompactMode = LyricsWindowStatus.IsAlwaysHidePlayingBar || RootGrid.ActualWidth < 180 ||
                                      RootGrid.ActualHeight <= 72;

        NowPlayingBar.ShowTime = NowPlayingBar.ShowVolumeButton = NowPlayingBar.ShowMoreButton =
            NowPlayingBar.IsCompactMode || RootGrid.ActualWidth > 350;
    }

    private void UpdateTopCommandGridStatus()
    {
        if (RootGrid.ActualWidth < 400)
        {
            TopCenterCommandGrid.Visibility = Visibility.Visible;
            if (TopCommandGrid.Children.Contains(TopLeftCommandGrid))
                TopCommandGrid.Children.Remove(TopLeftCommandGrid);

            if (TopCommandGrid.Children.Contains(TopRightCommandGrid))
                TopCommandGrid.Children.Remove(TopRightCommandGrid);

            if (!TopCommandFlyoutContainer.Children.Contains(TopLeftCommandGrid))
                TopCommandFlyoutContainer.Children.Add(TopLeftCommandGrid);

            if (!TopCommandFlyoutContainer.Children.Contains(TopRightCommandGrid))
                TopCommandFlyoutContainer.Children.Add(TopRightCommandGrid);
        }
        else
        {
            TopCenterCommandGrid.Visibility = Visibility.Collapsed;
            TopCommandFlyoutContainer.Children.Clear();
            if (!TopCommandGrid.Children.Contains(TopLeftCommandGrid)) TopCommandGrid.Children.Add(TopLeftCommandGrid);

            if (!TopCommandGrid.Children.Contains(TopRightCommandGrid))
                TopCommandGrid.Children.Add(TopRightCommandGrid);
        }
    }

    private void StartOverlayInputHelper()
    {
        if (_overlayInputHelper != null) return;

        _overlayInputHelper = new OverlayInputHelper(this);
        _overlayInputHelper.Register(RootGrid);
        _overlayInputHelper.Register(LockToggleButtonContainer);
        if (LyricsWindowStatus.KeepNowPlayingBarInteractiveWhenLocked) _overlayInputHelper.Register(NowPlayingBar);

        _overlayInputHelper.OnInteractiveAreaMoved = args =>
        {
            if (args.Elements.Contains(LockToggleButtonContainer) || args.Elements.Contains(NowPlayingBar))
            {
                _windowManagerProvider.SetIsClickThrough(this, false);
            }
            else
            {
                UnlockButton.Opacity = 1;
                _windowManagerProvider.SetIsClickThrough(this, true);
            }
        };
        _overlayInputHelper.OnInteractiveAreaExited = () =>
        {
            UnlockButton.Opacity = 0;
            _windowManagerProvider.SetIsClickThrough(this, true);
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
        if (LyricsWindowStatus.IsLocked) UnlockButton.Opacity = 1;
    }

    private void UnlockButton_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (LyricsWindowStatus.IsLocked) UnlockButton.Opacity = 0;
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
            this.SetWindowPresenter(AppWindowPresenterKind.FullScreen);
        else if (ExitFullscreenFontIcon.Opacity == 1) this.SetWindowPresenter(AppWindowPresenterKind.Overlapped);
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (EnterMaximizeFontIcon.Opacity == 1)
            this.Maximize();
        else if (ExitMaximizeFontIcon.Opacity == 1) this.Restore();
    }

    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        InitStatus();
        RootGrid.XamlRoot?.Changed += XamlRoot_Changed;
        OnTitleBarAreaChanged();
    }

    private void XamlRoot_Changed(XamlRoot sender, XamlRootChangedEventArgs args)
    {
        OnTitleBarAreaChanged();
    }
}