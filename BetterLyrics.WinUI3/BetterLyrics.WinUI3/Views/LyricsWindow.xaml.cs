// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services;
using CommunityToolkit.Mvvm.DependencyInjection;
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
        private readonly WindowMessageMonitor _wmm;

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
            WindowHelper.ExitApp();
        }

        public void UpdateTitleBarArea()
        {
            if (_settingsService.IsDragEverywhereEnabled)
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

        public void AutoSelectLyricsMode(AutoStartWindowType? type = null, bool? autoLook = null)
        {
            type ??= _settingsService.AutoStartWindowType;
            switch (type!)
            {
                case AutoStartWindowType.StandardMode:
                    AppWindow.MoveAndResize(new Windows.Graphics.RectInt32(
                        _settingsService.StandardWindowLeft,
                        _settingsService.StandardWindowTop,
                        _settingsService.StandardWindowWidth,
                        _settingsService.StandardWindowHeight));
                    break;
                case AutoStartWindowType.DockMode:
                    DockFlyoutItem.IsChecked = true;
                    ViewModel.ToggleDockModeCommand.Execute(null);
                    break;
                case AutoStartWindowType.DesktopMode:
                    DesktopFlyoutItem.IsChecked = true;
                    ViewModel.ToggleDesktopModeCommand.Execute(null);
                    if (autoLook == null && _settingsService.AutoLockOnDesktopMode)
                    {
                        ViewModel.ToggleLockWindowCommand.Execute(null);
                    }
                    break;
                default:
                    break;
            }
        }

        private void AOTFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var overlappedPresenter = (OverlappedPresenter)AppWindow.Presenter;
            overlappedPresenter.IsAlwaysOnTop = !overlappedPresenter.IsAlwaysOnTop;
        }

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (args.DidPresenterChange)
                UpdateTitleBarWindowButtonsVisibility();

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
                    if (ViewModel.IsDesktopMode)
                    {
                        _settingsService.DesktopWindowLeft = rect.X;
                        _settingsService.DesktopWindowTop = rect.Y;
                        _settingsService.DesktopWindowWidth = size.Width;
                        _settingsService.DesktopWindowHeight = size.Height;
                    }
                    else if (ViewModel.IsDockMode)
                    {

                    }
                    else
                    {
                        _settingsService.StandardWindowLeft = rect.X;
                        _settingsService.StandardWindowTop = rect.Y;
                        _settingsService.StandardWindowWidth = size.Width;
                        _settingsService.StandardWindowHeight = size.Height;
                    }
                }
            }
        }

        private void FullScreenFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            switch (AppWindow.Presenter.Kind)
            {
                case AppWindowPresenterKind.Default:
                    break;
                case AppWindowPresenterKind.CompactOverlay:
                    break;
                case AppWindowPresenterKind.FullScreen:
                    AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
                    break;
                case AppWindowPresenterKind.Overlapped:
                    AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                    break;
                default:
                    break;
            }
        }

        private void MiniFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (MiniFlyoutItem.IsChecked)
            {
                AppWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);
            }
            else
            {
                AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
            }
        }

        private void SettingsMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            WindowHelper.OpenWindow<SettingsWindow>();
        }

        private void UpdateTitleBarWindowButtonsVisibility()
        {
            switch (AppWindow.Presenter.Kind)
            {
                case AppWindowPresenterKind.Default:
                    break;
                case AppWindowPresenterKind.CompactOverlay:
                    AOTFlyoutItem.Visibility = DesktopFlyoutItem.Visibility = FullScreenFlyoutItem.Visibility = DockFlyoutItem.Visibility =
                    ClickThroughButton.Visibility = Visibility.Collapsed;

                    ViewModel.IsImmersiveMode = true;
                    break;
                case AppWindowPresenterKind.FullScreen:
                    AOTFlyoutItem.Visibility =
                    ClickThroughButton.Visibility =
                    DesktopFlyoutItem.Visibility =
                    MiniFlyoutItem.Visibility =
                    DockFlyoutItem.Visibility =
                        Visibility.Collapsed;
                    FullScreenFlyoutItem.IsChecked = true;
                    break;
                case AppWindowPresenterKind.Overlapped:
                    DockFlyoutItem.Visibility = Visibility.Visible;
                    var overlappedPresenter = (OverlappedPresenter)AppWindow.Presenter;
                    if (DockFlyoutItem.IsChecked)
                    {
                        overlappedPresenter.IsMinimizable =
                        overlappedPresenter.IsMaximizable = false;

                        AOTFlyoutItem.Visibility =
                        DesktopFlyoutItem.Visibility =
                        ClickThroughButton.Visibility =
                        FullScreenFlyoutItem.Visibility =
                        MiniFlyoutItem.Visibility =
                            Visibility.Collapsed;

                        ViewModel.IsImmersiveMode = true;
                    }
                    else if (DesktopFlyoutItem.IsChecked)
                    {
                        overlappedPresenter.IsMinimizable =
                        overlappedPresenter.IsMaximizable = false;

                        DockFlyoutItem.Visibility =
                        AOTFlyoutItem.Visibility =
                        FullScreenFlyoutItem.Visibility =
                        MiniFlyoutItem.Visibility =
                            Visibility.Collapsed;

                        ClickThroughButton.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        overlappedPresenter.IsMinimizable =
                        overlappedPresenter.IsMaximizable = true;

                        AOTFlyoutItem.Visibility =
                        DesktopFlyoutItem.Visibility =
                        DockFlyoutItem.Visibility =
                        MiniFlyoutItem.Visibility =
                        FullScreenFlyoutItem.Visibility =
                            Visibility.Visible;
                        FullScreenFlyoutItem.IsChecked = false;
                        ClickThroughButton.Visibility = Visibility.Collapsed;
                        AOTFlyoutItem.IsChecked = overlappedPresenter.IsAlwaysOnTop;

                        ViewModel.IsImmersiveMode = _settingsService.IsImmersiveMode;
                    }
                    break;
                default:
                    break;
            }
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
            ViewModel.ToggleLockWindowCommand.Execute(null);
        }

        private void DockFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleDockModeCommand.Execute(null);
        }

        private void DesktopFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleDesktopModeCommand.Execute(null);
            UpdateTitleBarWindowButtonsVisibility();
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
