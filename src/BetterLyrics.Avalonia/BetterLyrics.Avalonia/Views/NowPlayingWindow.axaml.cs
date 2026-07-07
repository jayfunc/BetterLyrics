using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using BetterLyrics.Core.Constants;
using BetterLyrics.Core.Enums;
using BetterLyrics.Core.Extensions;
using BetterLyrics.Core.Helpers;
using BetterLyrics.Core.Interfaces.Providers;
using BetterLyrics.Core.Interfaces.Services;
using BetterLyrics.Core.Models.Domain;
using BetterLyrics.Core.Models.Settings;
using BetterLyrics.Avalonia.Extensions;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Logging;

// 注意：你需要确保你的 WindowMessageMonitor 等工具能够接受 IntPtr (HWND)
// 因为 Avalonia 不再有原生的 WinUI3 Window 对象传递给底层。

namespace BetterLyrics.Avalonia.Views;

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

    private AppColor _backdropAccentColor = Core.Constants.Colors.Transparent;

    // TODO

    //private OverlayInputHelper? _overlayInputHelper;
    //private TaskbarHook? _taskbarHook;
    //private WindowMessageMonitor? _wmm;

    public NowPlayingWindow(LyricsWindowStatus status)
    {
        InitializeComponent();

        LyricsWindowStatus = status;
        NowPlayingPage.LyricsWindowStatus = LyricsWindowStatus;
        NowPlayingBar.LyricsWindowStatus = LyricsWindowStatus;

        this.Init(title: status.Name, titleBarHeightOption: 0, backdropType: BackdropType.Transparent);

        Closing += SettingsWindow_Closing;

        WeakReferenceMessenger.Default.RegisterAll(this);

        RequestUpdateAlbumArtThemeColors();

        LyricsWindowStatus.WindowStatus = WindowStatus.Opened;
    }

    public LyricsWindowStatus LyricsWindowStatus { get; }

    // 由于涉及到获取 HWND，必须在控件彻底附加到视觉树后初始化 Hook
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);

        var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        if (hwnd != IntPtr.Zero)
        {
            // 修改 WindowMessageMonitor 使其接收 IntPtr

            // TODO

            //_wmm = new WindowMessageMonitor(hwnd);
            //_wmm.WindowMessageReceived += Wmm_WindowMessageReceived;
        }

        // 替代 AppWindow_Changed，直接订阅 Avalonia 的事件
        PositionChanged += OnWindowPositionOrSizeChanged;
        SizeChanged += OnWindowPositionOrSizeChanged;
    }

    // 监听 WindowState 的改变（替代 AppWindowPresenter 的变化）
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == WindowStateProperty)
        {
            UpdatePresenterIcons();
        }
    }

    private void UpdatePresenterIcons()
    {
        if (WindowState == WindowState.Normal)
        {
            EnterMaximizeFontIcon.IsVisible = true;
            ExitMaximizeFontIcon.IsVisible = false;
            LyricsWindowStatus.IsMaximized = false;

            EnterFullscreenFontIcon.IsVisible = true;
            ExitFullscreenFontIcon.IsVisible = false;
            MaximizeButton.IsVisible = true;
            AOTButton.IsVisible = true;
            MinimizeButton.IsVisible = true;
            LockButton.IsVisible = true;

            LyricsWindowStatus.IsFullscreen = false;
        }
        else if (WindowState == WindowState.Maximized)
        {
            EnterMaximizeFontIcon.IsVisible = false;
            ExitMaximizeFontIcon.IsVisible = true;
            LyricsWindowStatus.IsMaximized = true;

            EnterFullscreenFontIcon.IsVisible = true;
            ExitFullscreenFontIcon.IsVisible = false;
            MaximizeButton.IsVisible = true;
            AOTButton.IsVisible = true;
            MinimizeButton.IsVisible = true;
            LockButton.IsVisible = true;

            LyricsWindowStatus.IsFullscreen = false;
        }
        else if (WindowState == WindowState.FullScreen)
        {
            EnterMaximizeFontIcon.IsVisible = false;
            ExitMaximizeFontIcon.IsVisible = false;

            EnterFullscreenFontIcon.IsVisible = false;
            ExitFullscreenFontIcon.IsVisible = true;
            MaximizeButton.IsVisible = false;
            AOTButton.IsVisible = false;
            MinimizeButton.IsVisible = false;
            LockButton.IsVisible = false;

            LyricsWindowStatus.IsMaximized = false;
            LyricsWindowStatus.IsFullscreen = true;
        }
    }

    private void OnWindowPositionOrSizeChanged(object? sender, EventArgs e)
    {
        var size = ClientSize;
        var rect = Position;

        if (!LyricsWindowStatus.IsWallpaper && (LyricsWindowStatus.IsMaximized || LyricsWindowStatus.IsFullscreen))
        {
            // Do nothing
        }
        else if (LyricsWindowStatus.IsWallpaper && LyricsWindowStatus.IsLocked)
        {
            // Do nothing
        }
        else
        {
            LyricsWindowStatus.WindowBounds = new AppRect(rect.X, rect.Y, size.Width, size.Height);
            UpdateMonitorNameAndBounds();
        }
    }

    public void Receive(PropertyChangedMessage<AppColor> message)
    {
        // 简化了逻辑层叠
        if (message.Sender == LyricsWindowStatus.LyricsStyleSettings ||
            message.Sender == LyricsWindowStatus.LyricsBackgroundSettings)
        {
            RequestUpdateAlbumArtThemeColors();
        }
    }

    public void Receive(PropertyChangedMessage<AppTheme> message)
    {
        if (message.Sender == LyricsWindowStatus && message.PropertyName == nameof(LyricsWindowStatus.WindowTheme))
            RequestUpdateAlbumArtThemeColors();
    }

    public void Receive(PropertyChangedMessage<bool> message)
    {
        if (message.Sender is IGsmtcService && message.PropertyName == nameof(IGsmtcService.CurrentIsPlaying))
        {
            OnAutoShowOrHideWindowChanged();
        }
        else if (message.Sender == LyricsWindowStatus)
        {
            switch (message.PropertyName)
            {
                case nameof(LyricsWindowStatus.IsShownInSwitchers):
                    OnIsShownInSwitchersChanged();
                    break;
                case nameof(LyricsWindowStatus.IsAlwaysOnTop):
                    OnIsAlwaysOnTopChanged();
                    break;
                case nameof(LyricsWindowStatus.IsAlwaysOnTopPolling):
                    OnIsAlwaysOnTopPollingChanged();
                    break;
                case nameof(LyricsWindowStatus.IsLocked):
                case nameof(LyricsWindowStatus.IsAlwaysHideUnlockButton):
                case nameof(LyricsWindowStatus.KeepNowPlayingBarInteractiveWhenLocked):
                    OnIsLockedChanged();
                    break;
                case nameof(LyricsWindowStatus.HideWindowWhenPaused):
                case nameof(LyricsWindowStatus.HideWindowWhenNullSession):
                    OnAutoShowOrHideWindowChanged();
                    break;
                case nameof(LyricsWindowStatus.IsAdaptToEnvironment):
                    OnIsAdaptToEnvironmentChanged();
                    RequestUpdateAlbumArtThemeColors();
                    break;
                case nameof(LyricsWindowStatus.IsAdaptToAlbumArt):
                    RequestUpdateAlbumArtThemeColors();
                    break;
                case nameof(LyricsWindowStatus.IsAlwaysHidePlayingBar):
                    UpdateNowPlayingBarStatus();
                    break;
            }
        }
    }

    public void Receive(PropertyChangedMessage<byte[]?> message)
    {
        if (message.Sender is IGsmtcService && message.PropertyName == nameof(IGsmtcService.AlbumArtBytes))
            RequestUpdateAlbumArtThemeColors();
    }

    public void Receive(PropertyChangedMessage<DockPlacement> message)
    {
        if (message.Sender == LyricsWindowStatus && message.PropertyName == nameof(LyricsWindowStatus.DockPlacement))
            OnWorkAreaChanged();
    }

    public void Receive(PropertyChangedMessage<double> message)
    {
        if (message.Sender == LyricsWindowStatus && message.PropertyName == nameof(LyricsWindowStatus.DockHeight))
            OnWorkAreaChanged();
    }

    public void Receive(PropertyChangedMessage<LyricsFontColorType> message)
    {
        RequestUpdateAlbumArtThemeColors();
    }

    public void Receive(PropertyChangedMessage<MediaSourceProviderInfo?> message)
    {
        if (message.Sender is IGsmtcService && message.PropertyName == nameof(IGsmtcService.CurrentMediaSourceProviderInfo))
            OnAutoShowOrHideWindowChanged();
    }

    public void Receive(PropertyChangedMessage<PaletteGeneratorType> message)
    {
        if (message.Sender == LyricsWindowStatus && message.PropertyName == nameof(LyricsWindowStatus.PaletteGeneratorType))
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
        // TODO

        //if (message.Sender == LyricsWindowStatus && message.PropertyName == nameof(LyricsWindowStatus.TaskbarPlacement))
        //    _taskbarHook?.UpdatePlacement(LyricsWindowStatus.TaskbarPlacement);
    }

    public void Receive(PropertyChangedMessage<TitleBarArea> message)
    {
        if (message.Sender == LyricsWindowStatus && message.PropertyName == nameof(LyricsWindowStatus.TitleBarArea))
            OnTitleBarAreaChanged();
    }

    // TODO

    //private void Wmm_WindowMessageReceived(object? sender, WindowMessageEventArgs e)
    //{
    //    var msgId = e.Message.MessageId;
    //    if (msgId == (uint)Core.Constants.Message.WM_APPBAR_CALLBACK)
    //    {
    //        var notification = (Shell32.ABN)e.Message.WParam;
    //        switch (notification)
    //        {
    //            case Shell32.ABN.ABN_POSCHANGED:
    //            case Shell32.ABN.ABN_STATECHANGE:
    //            case Shell32.ABN.ABN_FULLSCREENAPP:
    //                _windowManagerProvider.MoveAndResize(this, LyricsWindowStatus.GetAppBarBounds());
    //                break;
    //        }
    //        e.Handled = true;
    //    }
    //    else
    //    {
    //        var msg = (WindowMessage)msgId;
    //        if (msg == WindowMessage.WM_SETTINGCHANGE)
    //        {
    //            var changedSetting = Marshal.PtrToStringUni(e.Message.LParam);
    //            if (changedSetting == "Desktop" && LyricsWindowStatus.IsWallpaper && LyricsWindowStatus.IsLocked)
    //            {
    //                _appUIThreadProvider.Execute(() =>
    //                {
    //                    var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
    //                    WorkerWHook.UnpinFromDesktop(hwnd);
    //                    WorkerWHook.PinToDesktop(hwnd);
    //                });
    //            }
    //        }
    //    }
    //}

    private void InitStatus()
    {
        OnIsShownInSwitchersChanged();
        OnIsAlwaysOnTopChanged();
        OnTitleBarAreaChanged();
        OnIsAdaptToEnvironmentChanged();

        if (LyricsWindowStatus.IsPinToTaskbar || LyricsWindowStatus.IsWallpaper)
        {
            _windowManagerProvider.MoveAndResize(this, LyricsWindowStatus.WindowBounds);
            OnIsLockedChanged();
            Show();
        }
        else if (LyricsWindowStatus.IsWorkArea)
        {
            _windowManagerProvider.SetIsAppBar(this, true);
            LyricsWindowStatus.IsLocked = true;
            UpdateBackdropAccentColor();
            OnIsLockedChanged();
            Show();
        }
        else
        {
            _windowManagerProvider.MoveAndResize(this, LyricsWindowStatus.WindowBounds);
            OnIsLockedChanged();
            if (LyricsWindowStatus.IsFullscreen)
            {
                Show();
                WindowState = WindowState.FullScreen;
            }
            else if (LyricsWindowStatus.IsMaximized)
            {
                WindowState = WindowState.Maximized;
                Show();
            }
            else
            {
                Show();
            }
        }

        OnAutoShowOrHideWindowChanged();
    }

    public void UpdateBackdropAccentColor()
    {
        var oldValue = _backdropAccentColor;
        var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;

        // 传递 HWND
        var newValue = ColorHelper.GetAccentColor(hwnd, LyricsWindowStatus.EnvironmentSampleMode);
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
            var result = await _gsmtcService.CalculateAlbumArtThemeColorsAsync(LyricsWindowStatus, _backdropAccentColor);

            _appUIThreadProvider.Execute(() =>
            {
                NowPlayingPage.LyricsWindowStatus?.WindowPalette = result;
                // 转换主题类型逻辑
                // RootGrid.RequestedThemeVariant = ...
            });
        });
    }

    private void UpdateMonitorNameAndBounds()
    {
        var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;

        // TODO

        //var mointor = MonitorHook.GetMonitorInfoExFromWindow(hwnd);
        //LyricsWindowStatus.MonitorDeviceName = mointor.szDevice;
        //LyricsWindowStatus.MonitorBounds = mointor.rcMonitor.ToAppRect();
    }

    private void OnIsShownInSwitchersChanged()
    {
        ShowInTaskbar = LyricsWindowStatus.IsShownInSwitchers;
    }

    private void OnIsAlwaysOnTopChanged()
    {
        Topmost = LyricsWindowStatus.IsAlwaysOnTop;
        PinFillFontIcon.IsVisible = LyricsWindowStatus.IsAlwaysOnTop;
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
                    _appUIThreadProvider.Execute(() => { Topmost = true; });
            });
            LyricsWindowStatus.IsAlwaysOnTopPollingTimerRunning = true;
        }
    }

    private void OnIsLockedChanged()
    {
        if (LyricsWindowStatus.IsBorderlessWhenLocked)
            _windowManagerProvider.SetIsBorderless(this, LyricsWindowStatus.IsLocked);

        if (!LyricsWindowStatus.IsWallpaper)
            _windowManagerProvider.SetIsBorderless(this, LyricsWindowStatus.IsLocked);

        UnlockButton.IsVisible = !LyricsWindowStatus.IsAlwaysHideUnlockButton;
        StopOverlayInputHelper();

        var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;

        if (LyricsWindowStatus.IsLocked)
        {
            LockToggleButtonContainer.IsVisible = true;
            if (LyricsWindowStatus.IsWallpaper)
            {
                // TODO

                //WorkerWHook.PinToDesktop(hwnd);
            }
            else
            {
                if (LyricsWindowStatus.IsPinToTaskbar) PinToTaskbar();

                if (!LyricsWindowStatus.IsAlwaysHideUnlockButton || LyricsWindowStatus.KeepNowPlayingBarInteractiveWhenLocked)
                    StartOverlayInputHelper();
            }
        }
        else
        {
            LockToggleButtonContainer.IsVisible = false;
            UnlockButton.Opacity = 0;
            if (LyricsWindowStatus.IsWallpaper)
            {
                // TODO

                //WorkerWHook.UnpinFromDesktop(hwnd);
            }
            else if (LyricsWindowStatus.IsPinToTaskbar)
            {
                // TODO

                //_taskbarHook?.Dispose();
                //_taskbarHook = null;
            }
        }
    }

    private void PinToTaskbar()
    {
        // TODO

        //_taskbarHook?.Dispose();
        //_taskbarHook = null;

        //var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        //_taskbarHook = new TaskbarHook(hwnd, LyricsWindowStatus.TaskbarPlacement, LyricsWindowStatus.MonitorBounds);
    }

    private void OnAutoShowOrHideWindowChanged()
    {
        var status = LyricsWindowStatus;

        if (status.HideWindowWhenPaused || status.HideWindowWhenNullSession)
            _ = _visibilityDebouncer.RunAsync(async () =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    if (status.WindowStatus == WindowStatus.HiddenBySystem)
                    {
                        if ((status.HideWindowWhenPaused && _gsmtcService.CurrentIsPlaying)
                            || (status.HideWindowWhenNullSession && _gsmtcService.CurrentMediaSourceProviderInfo != null))
                        {
                            _windowManagerProvider.OpenOrShowWindow<NowPlayingWindow>(status);
                            if (status.IsWorkArea)
                            {
                                _windowManagerProvider.SetIsAppBar(this, true);
                                _windowManagerProvider.MoveAndResize(this, status.GetAppBarBounds());
                            }

                            if (status.IsLocked && status.IsWallpaper && (!status.IsAlwaysHideUnlockButton || status.KeepNowPlayingBarInteractiveWhenLocked))
                                RestartOverlayInputHelper();
                        }
                    }
                    else if (status.WindowStatus == WindowStatus.Opened)
                    {
                        if ((status.HideWindowWhenPaused && !_gsmtcService.CurrentIsPlaying)
                            || (status.HideWindowWhenNullSession && _gsmtcService.CurrentMediaSourceProviderInfo == null))
                        {
                            _windowManagerProvider.HideWindow(WindowStatus.HiddenBySystem);
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
            _backdropAccentColor = Core.Constants.Colors.Transparent;
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

    public void SetTitleBarArea(TitleBarArea titleBarArea)
    {
        // 💡 迁移提醒：Avalonia 并没有直接暴露设置拖拽矩形的 API。
        // 如果你需要允许控件空白处拖拽窗口，推荐的做法是为对应的控件附加 PointerPressed 事件，并在事件中调用 this.BeginMoveDrag(e);
        // 如果该窗体开启了 ExtendClientAreaToDecorationsHint="True"，则 Avalonia 也会自动捕获标题栏空白处的拖拽行为。
    }

    private void SettingsWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (_settingsService.AppSettings.GeneralSettings.ExitOnLyricsWindowClosed)
            _windowManagerProvider.ExitApp();
        else
            _windowManagerProvider.PrepareWindowClosing(this);
    }

    private void Window_Closed(object? sender, EventArgs e)
    {
        Closed -= Window_Closed;
        WeakReferenceMessenger.Default.UnregisterAll(this);
        StopOverlayInputHelper();

        PositionChanged -= OnWindowPositionOrSizeChanged;
        SizeChanged -= OnWindowPositionOrSizeChanged;

        // TODO

        //_wmm?.Dispose();
        //_wmm = null;

        _alwaysOnTopPoller.Stop();
        _alwaysOnTopPoller.Dispose();
        LyricsWindowStatus.IsAlwaysOnTopPollingTimerRunning = false;

        _underlayColorPoller.Stop();
        _underlayColorPoller.Dispose();
        LyricsWindowStatus.IsUnderlayColorTimerRunning = false;

        _visibilityDebouncer.Dispose();
        _albumArtThemeColorsDebounder.Dispose();

        // TODO

        //_taskbarHook?.Dispose();
        //_taskbarHook = null;
    }

    private void MusicGalleryButton_Click(object? sender, RoutedEventArgs e)
    {
        _windowManagerProvider.OpenOrShowWindow(WindowType.MusicGalleryWindow);
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_settingsService.AppSettings.GeneralSettings.ExitOnLyricsWindowClosed)
            _windowManagerProvider.ExitApp();
        else
            _windowManagerProvider.CloseWindow(this);
    }

    private void LyricsWindowSwitchButton_Click(object? sender, RoutedEventArgs e)
    {
        _windowManagerProvider.OpenOrShowWindow(WindowType.LyricsWindowSwitchWindow);
    }

    private void SettingsWindowButton_Click(object? sender, RoutedEventArgs e)
    {
        _windowManagerProvider.OpenOrShowWindow<SettingsWindow>();
    }

    private void MinimizeButton_Click(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void RootGrid_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateNowPlayingBarStatus();
        UpdateTopCommandGridStatus();
        OnTitleBarAreaChanged();
    }

    private void UpdateNowPlayingBarStatus()
    {
        NowPlayingBar.IsCompactMode = LyricsWindowStatus.IsAlwaysHidePlayingBar || RootGrid.Bounds.Width < 180 ||
                                      RootGrid.Bounds.Height <= 72;

        NowPlayingBar.ShowTime = NowPlayingBar.ShowVolumeButton = NowPlayingBar.ShowMoreButton =
            NowPlayingBar.IsCompactMode || RootGrid.Bounds.Width > 350;
    }

    private void UpdateTopCommandGridStatus()
    {
        if (RootGrid.Bounds.Width < 400)
        {
            TopCenterCommandGrid.IsVisible = true;
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
            TopCenterCommandGrid.IsVisible = false;
            TopCommandFlyoutContainer.Children.Clear();
            if (!TopCommandGrid.Children.Contains(TopLeftCommandGrid)) TopCommandGrid.Children.Add(TopLeftCommandGrid);
            if (!TopCommandGrid.Children.Contains(TopRightCommandGrid)) TopCommandGrid.Children.Add(TopRightCommandGrid);
        }
    }

    private void StartOverlayInputHelper()
    {
        // TODO

        //if (_overlayInputHelper != null) return;

        //var hwnd = TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
        //_overlayInputHelper = new OverlayInputHelper(hwnd); // 假设传递 hwnd 给它
        //_overlayInputHelper.Register(RootGrid);
        //_overlayInputHelper.Register(LockToggleButtonContainer);
        //if (LyricsWindowStatus.KeepNowPlayingBarInteractiveWhenLocked) _overlayInputHelper.Register(NowPlayingBar);

        //_overlayInputHelper.OnInteractiveAreaMoved = args =>
        //{
        //    if (args.Elements.Contains(LockToggleButtonContainer) || args.Elements.Contains(NowPlayingBar))
        //    {
        //        _windowManagerProvider.SetIsClickThrough(this, false);
        //    }
        //    else
        //    {
        //        UnlockButton.Opacity = 1;
        //        _windowManagerProvider.SetIsClickThrough(this, true);
        //    }
        //};
        //_overlayInputHelper.OnInteractiveAreaExited = () => { UnlockButton.Opacity = 0; };
        //_overlayInputHelper.Start();
        LyricsWindowStatus.IsOverlayInputHelperRunning = true;
    }

    public void StopOverlayInputHelper()
    {
        // TODO

        //_overlayInputHelper?.Stop();
        //_overlayInputHelper = null;
        LyricsWindowStatus.IsOverlayInputHelperRunning = false;
    }

    public void RestartOverlayInputHelper()
    {
        StopOverlayInputHelper();
        StartOverlayInputHelper();
    }

    private void UnlockButton_Click(object? sender, RoutedEventArgs e)
    {
        LyricsWindowStatus.IsLocked = false;
    }

    private void LockButton_Click(object? sender, RoutedEventArgs e)
    {
        LyricsWindowStatus.IsLocked = true;
    }

    private void AOTButton_Click(object? sender, RoutedEventArgs e)
    {
        LyricsWindowStatus.IsAlwaysOnTop = !LyricsWindowStatus.IsAlwaysOnTop;
    }

    private void FullscreenButton_Click(object? sender, RoutedEventArgs e)
    {
        if (EnterFullscreenFontIcon.IsVisible)
            WindowState = WindowState.FullScreen;
        else if (ExitFullscreenFontIcon.IsVisible)
            WindowState = WindowState.Normal;
    }

    private void MaximizeButton_Click(object? sender, RoutedEventArgs e)
    {
        if (EnterMaximizeFontIcon.IsVisible)
            WindowState = WindowState.Maximized;
        else if (ExitMaximizeFontIcon.IsVisible)
            WindowState = WindowState.Normal;
    }

    private void RootGrid_Loaded(object? sender, RoutedEventArgs e)
    {
        InitStatus();
        OnTitleBarAreaChanged();
    }
}