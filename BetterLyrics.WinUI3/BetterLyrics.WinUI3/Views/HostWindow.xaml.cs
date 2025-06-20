using System;
using BetterInAppLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using WinRT.Interop;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HostWindow : Window
    {
        public HostWindowViewModel ViewModel { get; private set; } =
            Ioc.Default.GetRequiredService<HostWindowViewModel>();

        private readonly ISettingsService _settingsService =
            Ioc.Default.GetRequiredService<ISettingsService>();

        public HostWindow(bool alwaysOnTop = false, bool clickThrough = false)
        {
            this.InitializeComponent();

            AppWindow.Changed += AppWindow_Changed;

            this.HideSystemTitleBarAndSetCustomTitleBar(TopCommandGrid);

            if (clickThrough)
                this.SetExtendedWindowStyle(
                    ExtendedWindowStyle.Transparent | ExtendedWindowStyle.Layered
                );

            if (alwaysOnTop)
                ((OverlappedPresenter)AppWindow.Presenter).IsAlwaysOnTop = true;
        }

        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (args.DidPresenterChange)
                UpdateTitleBarWindowButtonsVisibility();
        }

        public void Navigate(Type type)
        {
            RootFrame.Navigate(type);
        }

        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (RootFrame.SourcePageType == typeof(LyricsPage))
            {
                Application.Current.Exit();
            }
            else
            {
                AppWindow.Hide();
            }
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
                        FullScreenFlyoutItem.Visibility =
                        DockFlyoutItem.Visibility =
                            Visibility.Collapsed;
                    break;
                case AppWindowPresenterKind.FullScreen:
                    MinimiseButton.Visibility =
                        MaximiseButton.Visibility =
                        RestoreButton.Visibility =
                        AOTFlyoutItem.Visibility =
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
                            FullScreenFlyoutItem.Visibility =
                            MiniFlyoutItem.Visibility =
                                Visibility.Collapsed;
                    }
                    else
                    {
                        MinimiseButton.Visibility =
                            AOTFlyoutItem.Visibility =
                            MiniFlyoutItem.Visibility =
                            FullScreenFlyoutItem.Visibility =
                                Visibility.Visible;
                        FullScreenFlyoutItem.IsChecked = false;
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

        private void RootFrame_Navigated(object sender, NavigationEventArgs e)
        {
            AppWindow.Title = Title = App.ResourceLoader!.GetString(
                $"{e.SourcePageType.Name}Title"
            );
            if (e.SourcePageType == typeof(LyricsPage))
            {
                if (_settingsService.AutoStartWindowType == AutoStartWindowType.DockMode)
                {
                    DockFlyoutItem.IsChecked = true;
                    ViewModel.ToggleDockModeCommand.Execute(null);
                }
            }
        }

        private void AOTFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var overlappedPresenter = (OverlappedPresenter)AppWindow.Presenter;
            overlappedPresenter.IsAlwaysOnTop = !overlappedPresenter.IsAlwaysOnTop;
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
            WindowHelper.OpenSettingsWindow();
        }

        private void TopCommandGrid_PointerMoved(object sender, PointerRoutedEventArgs e) { }

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
    }
}
