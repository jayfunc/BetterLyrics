// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

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
            if (ViewModel.IsDesktopMode && (args.DidPositionChange || args.DidSizeChange))
                OnPosOrSizeChanged();
        }

        private void OnPosOrSizeChanged()
        {
            var rect = AppWindow.Position;
            var size = AppWindow.Size;

            _settingsService.DesktopWindowLeft = rect.X;
            _settingsService.DesktopWindowTop = rect.Y;
            _settingsService.DesktopWindowWidth = size.Width;
            _settingsService.DesktopWindowHeight = size.Height;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            WindowHelper.ExitAllWindows();
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

        private void MaximiseButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
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

        private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            var point = e.GetCurrentPoint(RootGrid);
            double y = point.Position.Y;

            if (y >= 0 && y <= TopCommandGrid.ActualHeight + 5)
            {
                if (TopCommandGrid.Opacity == 0)
                {
                    TopCommandGrid.Opacity = .5;
                }
            }
            else
            {
                if (TopCommandGrid.Opacity == .5)
                {
                    TopCommandGrid.Opacity = 0;
                }
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
                    MinimiseButton.Visibility =
                        MaximiseButton.Visibility =
                        RestoreButton.Visibility =
                        AOTFlyoutItem.Visibility =
                        DesktopFlyoutItem.Visibility =
                        ClickThroughButton.Visibility =
                        FullScreenFlyoutItem.Visibility =
                        DockFlyoutItem.Visibility =
                            Visibility.Collapsed;
                    break;
                case AppWindowPresenterKind.FullScreen:
                    MinimiseButton.Visibility =
                        MaximiseButton.Visibility =
                        RestoreButton.Visibility =
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
                        MinimiseButton.Visibility =
                            MaximiseButton.Visibility =
                            RestoreButton.Visibility =
                            AOTFlyoutItem.Visibility =
                            DesktopFlyoutItem.Visibility =
                            ClickThroughButton.Visibility =
                            FullScreenFlyoutItem.Visibility =
                            MiniFlyoutItem.Visibility =
                                Visibility.Collapsed;
                    }
                    else if (DesktopFlyoutItem.IsChecked)
                    {
                        MinimiseButton.Visibility =
                            MaximiseButton.Visibility =
                            RestoreButton.Visibility =
                            DockFlyoutItem.Visibility =
                            AOTFlyoutItem.Visibility =
                            FullScreenFlyoutItem.Visibility =
                            MiniFlyoutItem.Visibility =
                                Visibility.Collapsed;

                        ClickThroughButton.Visibility = Visibility.Visible;
                    }
                    else
                    {
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
                    TopCommandGrid.Opacity = 0;
                    break;
                default:
                    break;
            }
        }
    }
}
