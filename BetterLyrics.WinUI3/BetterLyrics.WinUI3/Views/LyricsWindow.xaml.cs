// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class LyricsWindow : Window
    {
        private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();

        public LyricsWindow()
        {
            this.InitializeComponent();

            AppWindow.Changed += AppWindow_Changed;

            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
            Title = App.ResourceLoader!.GetString("LyricsPageTitle");
            SetTitleBar(TopCommandGrid);
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
                        _settingsService.StandardWindowHeight
                    ));
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
                        ViewModel.LockWindowCommand.Execute(null);
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
                var rect = AppWindow.Position;
                var size = AppWindow.Size;

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
            WindowHelper.OpenOrShowWindow<SettingsWindow>();
        }

        private void UpdateTitleBarWindowButtonsVisibility()
        {
            switch (AppWindow.Presenter.Kind)
            {
                case AppWindowPresenterKind.Default:
                    break;
                case AppWindowPresenterKind.CompactOverlay:
                    MinimiseButton.Visibility = MaximiseButton.Visibility = RestoreButton.Visibility =
                    AOTFlyoutItem.Visibility = DesktopFlyoutItem.Visibility = FullScreenFlyoutItem.Visibility = DockFlyoutItem.Visibility =
                    ClickThroughButton.Visibility = Visibility.Collapsed;

                    break;
                case AppWindowPresenterKind.FullScreen:
                    MinimiseButton.Visibility = MaximiseButton.Visibility = RestoreButton.Visibility =
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

                        MinimiseButton.Visibility = MaximiseButton.Visibility = RestoreButton.Visibility =
                        AOTFlyoutItem.Visibility =
                        DesktopFlyoutItem.Visibility =
                        ClickThroughButton.Visibility =
                        FullScreenFlyoutItem.Visibility =
                        MiniFlyoutItem.Visibility =
                            Visibility.Collapsed;

                    }
                    else if (DesktopFlyoutItem.IsChecked)
                    {
                        overlappedPresenter.IsMinimizable =
                        overlappedPresenter.IsMaximizable = false;

                        MinimiseButton.Visibility = MaximiseButton.Visibility = RestoreButton.Visibility =
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

                        MinimiseButton.Visibility =
                        AOTFlyoutItem.Visibility =
                        DesktopFlyoutItem.Visibility =
                        DockFlyoutItem.Visibility =
                        MiniFlyoutItem.Visibility =
                        FullScreenFlyoutItem.Visibility =
                            Visibility.Visible;
                        FullScreenFlyoutItem.IsChecked = false;
                        ClickThroughButton.Visibility = Visibility.Collapsed;
                        AOTFlyoutItem.IsChecked = overlappedPresenter.IsAlwaysOnTop;

                        if (overlappedPresenter.State == OverlappedPresenterState.Maximized)
                        {
                            MaximiseButton.Visibility = Visibility.Collapsed;
                            RestoreButton.Visibility = Visibility.Visible;
                        }
                        else if (overlappedPresenter.State == OverlappedPresenterState.Restored)
                        {
                            MaximiseButton.Visibility = Visibility.Visible;
                            RestoreButton.Visibility = Visibility.Collapsed;
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            WindowHelper.ExitAllWindows();
        }

        private void MaximiseButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }
        }

        private void MinimiseButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Minimize();
            }
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Restore();
            }
        }

        private void TopCommandGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            TopCommandGrid.Opacity = 1;
        }

        private void TopCommandGrid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            TopCommandGrid.Opacity = 0;
        }

        private void TipContainerCenter_Loaded(object sender, RoutedEventArgs e)
        {
            App.Current.LyricsWindowNotificationPanel = TipContainerCenter;
        }

        private void RootGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.IsMouseWithinWindow = true;
        }

        private void RootGrid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            ViewModel.IsMouseWithinWindow = false;
        }
    }
}
