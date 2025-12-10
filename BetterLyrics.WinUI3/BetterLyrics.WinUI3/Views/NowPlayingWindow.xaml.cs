// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Extensions;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Hooks;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
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
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.UI;
using WinRT.Interop;
using WinUIEx.Messaging;

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
        IRecipient<PropertyChangedMessage<Color>>
    {
        private ForegroundWindowHook? _fgWindowWatcher = null;
        private OverlayInputHelper? _overlayInputHelper;
        private TaskbarHook? _taskbarHook;

        private DispatcherQueueTimer? _fgWindowWatcherTimer = null;

        private Color _backdropAccentColor = Colors.Transparent;

        public LyricsWindowStatus LyricsWindowStatus { get; private set; }

        public NowPlayingWindowViewModel ViewModel { get; private set; } = Ioc.Default.GetRequiredService<NowPlayingWindowViewModel>();
        private readonly IMediaSessionsService _mediaSessionsService = Ioc.Default.GetRequiredService<IMediaSessionsService>();
        private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

        public NowPlayingWindow(LyricsWindowStatus status)
        {
            this.InitializeComponent();

            _fgWindowWatcherTimer = DispatcherQueue.CreateTimer();
            _taskbarHook = new TaskbarHook(OnTaskbarFreeBoundsChanged);

            LyricsWindowStatus = status;
            NowPlayingPage.LyricsWindowStatus = LyricsWindowStatus;

            this.Init("LyricsPageTitle", TitleBarHeightOption.Collapsed, BackdropType.Transparent);

            AppWindow.Changed += AppWindow_Changed;
            AppWindow.Closing += AppWindow_Closing;

            SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(BackdropType.Transparent);

            WeakReferenceMessenger.Default.RegisterAll(this);

            UpdateAlbumArtThemeColors();
        }

        private void OnTaskbarFreeBoundsChanged(Events.TaskbarFreeBoundsChangedEventArgs obj)
        {
            this.MoveAndResize(obj.TaskbarFreeBounds);
        }

        public void InitStatus()
        {
            LyricsWindowStatus.UpdateMonitorBounds();

            OnIsWorkAreaChanged();
            this.MoveAndResize(LyricsWindowStatus.WindowBounds);
            OnIsShownInSwitchersChanged();
            OnIsAlwaysOnTopChanged();
            OnIsMaximizedChanged();
            OnIsFullscreenChanged();
            OnIsLockedChanged();
            OnAutoShowOrHideWindowChanged();
            OnTitleBarAreaChanged();
            OnIsPinToTaskbarChanged();

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
                UpdateAlbumArtThemeColors();
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
                    }, Constants.Time.DebounceTimeout);
                }
            );
            if (LyricsWindowStatus.IsAdaptToEnvironment)
            {
                UpdateBackdropAccentColor(hwnd);
            }
            OnIsAdaptToEnvironmentChanged();
        }

        private void UpdateAlbumArtThemeColors()
        {
            var result = _mediaSessionsService.CalculateAlbumArtThemeColors(LyricsWindowStatus, _backdropAccentColor);

            NowPlayingPage.AlbumArtThemeColors = result;
            RootGrid.RequestedTheme = result.ThemeType;
        }

        // ====

        private void OnIsWorkAreaChanged()
        {
            this.SetIsWorkArea(LyricsWindowStatus.IsWorkArea);
            if (LyricsWindowStatus.IsWorkArea)
            {
                LyricsWindowStatus.IsLocked = true;
                this.MoveAndResize(LyricsWindowStatus.GetWindowBoundsWhenWorkArea());
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
            if (LyricsWindowStatus.IsLocked && !LyricsWindowStatus.IsWorkArea)
            {
                LockToggleButtonContainer.Visibility = Visibility.Visible;
                StartOverlayInputHelper();
            }
            else
            {
                LockToggleButtonContainer.Visibility = Visibility.Collapsed;
                UnlockButton.Opacity = 0;
                StopOverlayInputHelper();
            }
            this.SetIsLocked(LyricsWindowStatus.IsLocked);
        }

        private void OnIsPinToTaskbarChanged()
        {
            if (LyricsWindowStatus.IsPinToTaskbar)
            {
                _taskbarHook = new(OnTaskbarFreeBoundsChanged);
            }
            else
            {
                _taskbarHook?.Dispose();
                _taskbarHook = null;
            }
        }

        private void OnIsFullscreenChanged()
        {
            this.SetIsFullscreen(LyricsWindowStatus.IsFullscreen);
            EnterFullscreenFontIcon.Opacity = LyricsWindowStatus.IsFullscreen ? 0 : 1;
            ExitFullscreenFontIcon.Opacity = LyricsWindowStatus.IsFullscreen ? 1 : 0;
            MaximizeButton.Visibility = LyricsWindowStatus.IsFullscreen ? Visibility.Collapsed : Visibility.Visible;
            AOTButton.Visibility = LyricsWindowStatus.IsFullscreen ? Visibility.Collapsed : Visibility.Visible;
            MinimizeButton.Visibility = LyricsWindowStatus.IsFullscreen ? Visibility.Collapsed : Visibility.Visible;
            LockButton.Visibility = LyricsWindowStatus.IsFullscreen ? Visibility.Collapsed : Visibility.Visible;
        }

        private void OnIsMaximizedChanged()
        {
            this.SetIsMaximized(LyricsWindowStatus.IsMaximized);
            EnterMaximizeFontIcon.Opacity = LyricsWindowStatus.IsMaximized ? 0 : 1;
            ExitMaximizeFontIcon.Opacity = LyricsWindowStatus.IsMaximized ? 1 : 0;
        }

        private void OnAutoShowOrHideWindowChanged()
        {
            this.SetLyricsWindowVisibilityByPlayingStatus(_mediaSessionsService.CurrentIsPlaying, DispatcherQueue);
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
                this.UpdateWorkArea();
                LyricsWindowStatus.IsLocked = true;
                this.MoveAndResize(LyricsWindowStatus.GetWindowBoundsWhenWorkArea());
            }
        }

        private void OnTitleBarAreaChanged()
        {
            this.SetTitleBarArea(LyricsWindowStatus.TitleBarArea);
        }

        // ====

        private void ExitOrClose()
        {
            _fgWindowWatcherTimer = null;
            _fgWindowWatcher?.Stop();
            _fgWindowWatcher = null;
            if (_settingsService.AppSettings.GeneralSettings.ExitOnLyricsWindowClosed)
            {
                WindowHook.ExitApp();
            }
            else
            {
                this.CloseWindow();
            }
        }

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
            ExitOrClose();
            args.Cancel = true;
        }

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (args.DidPositionChange || args.DidSizeChange)
            {
                var size = AppWindow.Size;
                var rect = AppWindow.Position;

                if (rect.X < 0 && rect.Y < 0 && rect.X + size.Width < 0 && rect.Y + size.Height < 0)
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
            ViewModel.TopCommandGridOpacity = 1f;
        }

        private void TopCommandGrid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.TopCommandGridOpacity = 0f;
        }

        private void MusicGalleryButton_Click(object sender, RoutedEventArgs e)
        {
            WindowHook.OpenOrShowWindow<MusicGalleryWindow>();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            ExitOrClose();
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
            NowPlayingBar.IsCompactMode = RootGrid.ActualWidth < 400 || RootGrid.ActualHeight < 100;
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
            _overlayInputHelper.OnInteractiveAreaMoved = (args) =>
            {
                if (args.Elements.Contains(LockToggleButtonContainer))
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

        private void StopOverlayInputHelper()
        {
            _overlayInputHelper?.Stop();
            _overlayInputHelper = null;
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
            if (message.Sender is IMediaSessionsService)
            {
                if (message.PropertyName == nameof(IMediaSessionsService.CurrentIsPlaying))
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
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.IsPinToTaskbar))
                {
                    OnIsPinToTaskbarChanged();
                }
            }
        }

        public void Receive(PropertyChangedMessage<BitmapImage?> message)
        {
            if (message.Sender is IMediaSessionsService)
            {
                if (message.PropertyName == nameof(IMediaSessionsService.AlbumArtBitmapImage))
                {
                    UpdateAlbumArtThemeColors();
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
            if (message.Sender == LyricsWindowStatus.LyricsBackgroundSettings)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.LyricsBackgroundSettings.LyricsBackgroundTheme))
                {
                    UpdateAlbumArtThemeColors();
                }
            }
        }

        public void Receive(PropertyChangedMessage<LyricsFontColorType> message)
        {
            if (message.Sender == LyricsWindowStatus.LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsBgFontColorType))
                {
                    UpdateAlbumArtThemeColors();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsFgFontColorType))
                {
                    UpdateAlbumArtThemeColors();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsStrokeFontColorType))
                {
                    UpdateAlbumArtThemeColors();
                }
            }
        }

        public void Receive(PropertyChangedMessage<Color> message)
        {
            if (message.Sender == LyricsWindowStatus.LyricsStyleSettings)
            {
                if (message.PropertyName == nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsCustomBgFontColor))
                {
                    UpdateAlbumArtThemeColors();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsCustomFgFontColor))
                {
                    UpdateAlbumArtThemeColors();
                }
                else if (message.PropertyName == nameof(LyricsWindowStatus.LyricsStyleSettings.LyricsCustomStrokeFontColor))
                {
                    UpdateAlbumArtThemeColors();
                }
            }
        }

    }
}
