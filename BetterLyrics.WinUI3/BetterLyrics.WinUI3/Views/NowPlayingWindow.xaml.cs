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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.UI;
using WinRT.Interop;
using WinUIEx.Messaging;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class NowPlayingWindow : Window,
        IRecipient<PropertyChangedMessage<bool>>,
        IRecipient<PropertyChangedMessage<BitmapDecoder?>>
    {
        private ForegroundWindowHook? _fgWindowWatcher = null;
        private OverlayInputHelper? _overlayInputHelper = null;
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

            LyricsWindowStatus = status;
            NowPlayingPage.LyricsWindowStatus = LyricsWindowStatus;

            this.Init("LyricsPageTitle", TitleBarHeightOption.Collapsed, BackdropType.Transparent);

            AppWindow.Changed += AppWindow_Changed;
            AppWindow.Closing += AppWindow_Closing;

            SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(BackdropType.Transparent);

            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<bool>>(this);
            WeakReferenceMessenger.Default.Register<PropertyChangedMessage<BitmapDecoder?>>(this);

            _ = UpdateAlbumArtThemeColorsAsync();
        }

        public async Task InitStatus()
        {
            LyricsWindowStatus.PropertyChanged += LyricsWindowStatus_PropertyChanged;

            LyricsWindowStatus.IsLyricsWindowStatusRefreshing = true;

            LyricsWindowStatus.UpdateMonitorBounds();

            this.SetIsWorkArea(LyricsWindowStatus.IsWorkArea);
            if (LyricsWindowStatus.IsWorkArea)
            {
                this.UpdateWorkArea();
            }
            await Task.Delay(300);

            this.SetIsShowInSwitchers(LyricsWindowStatus.IsShownInSwitchers);
            this.SetIsAlwaysOnTop(LyricsWindowStatus.IsAlwaysOnTop);
            PinFillFontIcon.Opacity = LyricsWindowStatus.IsAlwaysOnTop ? 1 : 0;

            this.SetIsFullscreen(LyricsWindowStatus.IsFullscreen);
            EnterFullscreenFontIcon.Opacity = LyricsWindowStatus.IsFullscreen ? 0 : 1;
            ExitFullscreenFontIcon.Opacity = LyricsWindowStatus.IsFullscreen ? 1 : 0;

            this.SetIsMaximized(LyricsWindowStatus.IsMaximized);
            EnterMaximizeFontIcon.Opacity = LyricsWindowStatus.IsMaximized ? 0 : 1;
            ExitMaximizeFontIcon.Opacity = LyricsWindowStatus.IsMaximized ? 1 : 0;

            this.SetIsLocked(LyricsWindowStatus.IsLocked);
            if (LyricsWindowStatus.IsLocked)
            {
                LockToggleButton.IsChecked = true;
                StartOverlayInputHelper();
            }
            else
            {
                LockToggleButton.IsChecked = false;
                StopOverlayInputHelper();
            }

            this.SetLyricsWindowVisibilityByPlayingStatus(DispatcherQueue);
            this.SetTitleBarArea(LyricsWindowStatus.TitleBarArea);

            // 下述代码可以删除，但是为了避免给用户造成操作上的疑虑，暂时保留
            if (LyricsWindowStatus.IsWorkArea)
            {
                LyricsWindowStatus.WindowBounds = LyricsWindowStatus.GetWindowBoundsWhenWorkArea();
            }

            this.MoveAndResize(LyricsWindowStatus.WindowBounds);

            LyricsWindowStatus.UpdateDemoWindowAndMonitorBounds();

            LyricsWindowStatus.IsLyricsWindowStatusRefreshing = false;
        }

        private async void LyricsWindowStatus_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Models.LyricsWindowStatus.IsWorkArea):
                    LyricsWindowStatus.IsLyricsWindowStatusRefreshing = true;
                    this.SetIsWorkArea(LyricsWindowStatus.IsWorkArea);
                    LyricsWindowStatus.IsLyricsWindowStatusRefreshing = false;
                    if (LyricsWindowStatus.IsWorkArea)
                    {
                        this.MoveAndResize(LyricsWindowStatus.GetWindowBoundsWhenWorkArea());
                    }
                    break;
                case nameof(Models.LyricsWindowStatus.DockHeight):
                case nameof(Models.LyricsWindowStatus.DockPlacement):
                case nameof(Models.LyricsWindowStatus.MonitorDeviceName):
                    LyricsWindowStatus.UpdateMonitorBounds();
                    if (LyricsWindowStatus.IsWorkArea)
                    {
                        LyricsWindowStatus.IsLyricsWindowStatusRefreshing = true;
                        this.UpdateWorkArea();
                        LyricsWindowStatus.IsLyricsWindowStatusRefreshing = false;
                        this.MoveAndResize(LyricsWindowStatus.GetWindowBoundsWhenWorkArea());
                    }
                    break;
                case nameof(Models.LyricsWindowStatus.IsShownInSwitchers):
                    this.SetIsShowInSwitchers(LyricsWindowStatus.IsShownInSwitchers);
                    break;
                case nameof(Models.LyricsWindowStatus.IsAlwaysOnTop):
                    this.SetIsAlwaysOnTop(LyricsWindowStatus.IsAlwaysOnTop);
                    PinFillFontIcon.Opacity = LyricsWindowStatus.IsAlwaysOnTop ? 1 : 0;
                    break;
                case nameof(Models.LyricsWindowStatus.IsLocked):
                    this.SetIsLocked(LyricsWindowStatus.IsLocked);
                    if (LyricsWindowStatus.IsLocked)
                    {
                        StartOverlayInputHelper();
                    }
                    else
                    {
                        StopOverlayInputHelper();
                    }
                    break;
                case nameof(Models.LyricsWindowStatus.IsFullscreen):
                    this.SetIsFullscreen(LyricsWindowStatus.IsFullscreen);
                    EnterFullscreenFontIcon.Opacity = LyricsWindowStatus.IsFullscreen ? 0 : 1;
                    ExitFullscreenFontIcon.Opacity = LyricsWindowStatus.IsFullscreen ? 1 : 0;
                    break;
                case nameof(Models.LyricsWindowStatus.IsMaximized):
                    this.SetIsMaximized(LyricsWindowStatus.IsMaximized);
                    EnterMaximizeFontIcon.Opacity = LyricsWindowStatus.IsMaximized ? 0 : 1;
                    ExitMaximizeFontIcon.Opacity = LyricsWindowStatus.IsMaximized ? 1 : 0;
                    break;
                case nameof(Models.LyricsWindowStatus.TitleBarArea):
                    this.SetTitleBarArea(LyricsWindowStatus.TitleBarArea);
                    break;
                case nameof(Models.LyricsWindowStatus.AutoShowOrHideWindow):
                    this.SetLyricsWindowVisibilityByPlayingStatus(DispatcherQueue);
                    break;
                case nameof(Models.LyricsWindowStatus.LyricsBackgroundSettings):
                    await UpdateAlbumArtThemeColorsAsync();
                    break;
                default:
                    break;
            }
        }

        public void UpdateBackdropAccentColor(nint hwnd)
        {
            _backdropAccentColor = Helper.ColorHelper.GetAccentColor(
                hwnd,
                LyricsWindowStatus.MonitorDeviceName,
                LyricsWindowStatus.EnvironmentSampleMode);
            UpdateAlbumArtThemeColorsAsync();
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
            _fgWindowWatcher.Start();
            UpdateBackdropAccentColor(hwnd);
        }

        private async Task UpdateAlbumArtThemeColorsAsync()
        {
            var result = await _mediaSessionsService.CalculateAlbumArtThemeColorsAsync(LyricsWindowStatus, _backdropAccentColor);

            NowPlayingPage.AlbumArtThemeColors = result;
            RootGrid.RequestedTheme = result.ThemeType;
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            ExitOrClose();
            args.Cancel = true;
        }

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

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (LyricsWindowStatus.IsLyricsWindowStatusRefreshing)
            {
                return;
            }

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
                    LyricsWindowStatus.WindowBounds = new Windows.Foundation.Rect(rect.X, rect.Y, size.Width, size.Height);
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
            NowPlayingBar.IsCompactMode = RootGrid.ActualWidth < 300 || RootGrid.ActualHeight < 100;
        }

        private void StartOverlayInputHelper()
        {
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
                    LockToggleButton.Opacity = 1;
                    this.SetIsClickThrough(true);
                }
            };
            _overlayInputHelper.OnInteractiveAreaExited = () =>
            {
                LockToggleButton.Opacity = 0;
            };
            _overlayInputHelper.Start();
        }

        private void StopOverlayInputHelper()
        {
            _overlayInputHelper?.Stop();
            _overlayInputHelper = null;
        }

        private void LockToggleButton_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            LockToggleButton.Opacity = 1;
        }

        private void LockToggleButton_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            LockToggleButton.Opacity = 0;
        }

        private void LockToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (LockToggleButton.IsChecked == true)
            {
                LyricsWindowStatus.IsLocked = true;
            }
            else
            {
                LyricsWindowStatus.IsLocked = false;
            }
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
                    this.SetLyricsWindowVisibilityByPlayingStatus(DispatcherQueue);
                }
            }
        }

        public async void Receive(PropertyChangedMessage<BitmapDecoder?> message)
        {
            if (message.Sender is IMediaSessionsService)
            {
                if (message.PropertyName == nameof(IMediaSessionsService.AlbumArtBitmapDecoder))
                {
                    if (message.NewValue is BitmapDecoder decoder)
                    {
                        await UpdateAlbumArtThemeColorsAsync();
                    }
                }
            }
        }

    }
}
