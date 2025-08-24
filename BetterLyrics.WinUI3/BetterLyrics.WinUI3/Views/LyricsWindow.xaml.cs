// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services.LiveStatesService;
using BetterLyrics.WinUI3.Services.SettingsService;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System;
using Vanara.PInvoke;
using WinRT.Interop;
using WinUIEx;
using WinUIEx.Messaging;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class LyricsWindow : Window
    {
        private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        private readonly ILiveStatesService _liveStatesService = Ioc.Default.GetRequiredService<ILiveStatesService>();
        private readonly WindowMessageMonitor _wmm;
        private bool _autoSelectLyricsModeOnRunning = true;

        public LyricsWindow()
        {
            this.InitializeComponent();

            AppWindow.SetIcons();

            AppWindow.Changed += AppWindow_Changed;

            ExtendsContentIntoTitleBar = true;
            UpdateTitleBarArea();

            Title = App.ResourceLoader!.GetString("LyricsPageTitle");

            _wmm = new WindowMessageMonitor(this);
            _wmm.WindowMessageReceived += Wmm_WindowMessageReceived;

            AppWindow.Closing += AppWindow_Closing;
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            ViewModel.ExitOrClose();
            args.Cancel = true;
        }

        public void UpdateTitleBarArea()
        {
            if (_settingsService.AppSettings.GeneralSettings.IsDragEverywhereEnabled)
            {
                SetTitleBar(RootGrid);
            }
            else
            {
                SetTitleBar(TopCommandGrid);
            }
        }

        private void Wmm_WindowMessageReceived(object? sender, WindowMessageEventArgs e)
        {
            if (e.Message.MessageId == (uint)User32.WindowMessage.WM_HOTKEY)
            {
                int id = (int)e.Message.WParam;
                GlobalHotKeyHelper.TryInvokeAction(id);
            }
        }

        public LyricsWindowViewModel ViewModel { get; private set; } = Ioc.Default.GetRequiredService<LyricsWindowViewModel>();

        public void AutoSelectLyricsMode(LyricsWindowMode? type = null)
        {
            type ??= _settingsService.AppSettings.GeneralSettings.AutoStartWindowType;
            switch (type!)
            {
                case LyricsWindowMode.StandardMode:
                    ViewModel.SetStandardModeTitleBarControlsStatus();
                    if (_settingsService.AppSettings.StandardModeSettings.IsMaximized)
                    {
                        // 记忆中最大化时避免设置窗口大小以便退出最大化后
                        // 不会四周都紧贴屏幕边缘影响操作
                        this.Maximize();
                    }
                    else
                    {
                        AppWindow.MoveAndResize(_settingsService.AppSettings.StandardModeSettings.WindowBounds.ToRectInt32());
                    }
                    break;
                case LyricsWindowMode.DockMode:
                    ViewModel.ToggleDockMode();
                    break;
                case LyricsWindowMode.DesktopMode:
                    ViewModel.ToggleDesktopMode();
                    break;
                case LyricsWindowMode.PictureInPictureMode:
                    ViewModel.TogglePictureInPictureMode();
                    break;
                default:
                    break;
            }
            _autoSelectLyricsModeOnRunning = false;

            var size = AppWindow.Size;
            var rect = AppWindow.Position;

            _liveStatesService.LiveStates.LyricsWindowBounds = new Windows.Foundation.Rect(rect.X, rect.Y, size.Width, size.Height);
        }

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (_autoSelectLyricsModeOnRunning) return;

            if (args.DidPositionChange || args.DidSizeChange)
            {
                var size = AppWindow.Size;
                var rect = AppWindow.Position;

                _liveStatesService.LiveStates.LyricsWindowBounds = new Windows.Foundation.Rect(rect.X, rect.Y, size.Width, size.Height);

                if (rect.X < 0 && rect.Y < 0 && rect.X + size.Width < 0 && rect.Y + size.Height < 0)
                {
                    return;
                }
                else
                {
                    switch (ViewModel.LiveStates.LyricsWindowMode)
                    {
                        case LyricsWindowMode.StandardMode:
                            if (AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
                            {
                                _settingsService.AppSettings.StandardModeSettings.WindowBounds = new Windows.Foundation.Rect(rect.X, rect.Y, size.Width, size.Height);
                                _settingsService.AppSettings.StandardModeSettings.IsMaximized = overlappedPresenter.State == OverlappedPresenterState.Maximized;
                            }
                            break;
                        case LyricsWindowMode.DockMode:
                            break;
                        case LyricsWindowMode.DesktopMode:
                            _settingsService.AppSettings.DesktopModeSettings.WindowBounds = new Windows.Foundation.Rect(rect.X, rect.Y, size.Width, size.Height);
                            break;
                        case LyricsWindowMode.PictureInPictureMode:
                            if (AppWindow.Presenter is CompactOverlayPresenter compactOverlayPresenter)
                            {
                                _settingsService.AppSettings.PictureInPictureModeSettings.WindowPosition = new Windows.Foundation.Point(rect.X, rect.Y);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void FullScreenFlyoutItem_Click(object sender, RoutedEventArgs e)
        {

            ViewModel.ToggleFullscreen();
        }

        private void PIPFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.TogglePictureInPictureMode();
        }

        private void SettingsMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            WindowHelper.OpenWindow<SettingsWindow>();
        }

        private void TopCommandGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (ViewModel.IsImmersiveMode)
            {
                ViewModel.TopCommandGridOpacity = 1f;
            }
        }

        private void TopCommandGrid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (ViewModel.IsImmersiveMode)
            {
                ViewModel.TopCommandGridOpacity = 0f;
            }
        }

        private void RootGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.IsMouseWithinWindow = true;
            e.Handled = true;
        }

        private void RootGrid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.IsMouseWithinWindow = false;
            e.Handled = true;
        }

        private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        private void ClickThroughButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleLockWindow();
        }

        private void DockFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleDockMode();
        }

        private void DesktopFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleDesktopMode();
        }

        private void MusicGalleryMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            WindowHelper.OpenWindow<MusicGalleryWindow>();
        }

        private void ExitAppMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            WindowHelper.ExitApp();
        }

        private void TipContainerCenter_Loaded(object sender, RoutedEventArgs e)
        {
            App.Current.LyricsWindowNotificationPanel = TipContainerCenter;
        }
    }
}
