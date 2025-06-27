// 2025/6/23 by Zhe Fang

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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame
    /// </summary>
    public sealed partial class HostWindow : Window
    {
        #region Fields

        /// <summary>
        /// Defines the _settingsService
        /// </summary>
        private readonly ISettingsService _settingsService =
            Ioc.Default.GetRequiredService<ISettingsService>();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="HostWindow"/> class.
        /// </summary>
        /// <param name="alwaysOnTop">The alwaysOnTop<see cref="bool"/></param>
        /// <param name="clickThrough">The clickThrough<see cref="bool"/></param>
        public HostWindow()
        {
            this.InitializeComponent();

            AppWindow.Changed += AppWindow_Changed;
            AppWindow.Closing += AppWindow_Closing;

            this.HideSystemTitleBarAndSetCustomTitleBar(TopCommandGrid);
        }

        private void CloseOrExit()
        {
            if (RootFrame.SourcePageType == typeof(LyricsPage))
            {
                App.Current.Exit();
            }
            else
            {
                AppWindow.Hide();
            }
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            args.Cancel = true;
            CloseOrExit();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the ViewModel
        /// </summary>
        public HostWindowViewModel ViewModel { get; private set; } =
            Ioc.Default.GetRequiredService<HostWindowViewModel>();

        #endregion

        #region Methods

        /// <summary>
        /// The Navigate
        /// </summary>
        /// <param name="type">The type<see cref="Type"/></param>
        public void Navigate(Type type)
        {
            RootFrame.Navigate(type);
        }

        /// <summary>
        /// The AOTFlyoutItem_Click
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/></param>
        private void AOTFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var overlappedPresenter = (OverlappedPresenter)AppWindow.Presenter;
            overlappedPresenter.IsAlwaysOnTop = !overlappedPresenter.IsAlwaysOnTop;
        }

        /// <summary>
        /// The AppWindow_Changed
        /// </summary>
        /// <param name="sender">The sender<see cref="AppWindow"/></param>
        /// <param name="args">The args<see cref="AppWindowChangedEventArgs"/></param>
        private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
        {
            if (args.DidPresenterChange)
                UpdateTitleBarWindowButtonsVisibility();
        }

        /// <summary>
        /// The CloseButton_Click
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/></param>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseOrExit();
        }

        /// <summary>
        /// The FullScreenFlyoutItem_Click
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/></param>
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

        /// <summary>
        /// The MaximiseButton_Click
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/></param>
        private void MaximiseButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
            }
        }

        /// <summary>
        /// The MiniFlyoutItem_Click
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/></param>
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

        /// <summary>
        /// The MinimiseButton_Click
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/></param>
        private void MinimiseButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Minimize();
            }
        }

        /// <summary>
        /// The RestoreButton_Click
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/></param>
        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Restore();
            }
        }

        /// <summary>
        /// The RootFrame_Navigated
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="NavigationEventArgs"/></param>
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

        /// <summary>
        /// The RootFrame_NavigationFailed
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="NavigationFailedEventArgs"/></param>
        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// The RootGrid_PointerMoved
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="PointerRoutedEventArgs"/></param>
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

        /// <summary>
        /// The SettingsMenuFlyoutItem_Click
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/></param>
        private void SettingsMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            WindowHelper.OpenSettingsWindow();
        }

        /// <summary>
        /// The TopCommandGrid_PointerMoved
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="PointerRoutedEventArgs"/></param>
        private void TopCommandGrid_PointerMoved(object sender, PointerRoutedEventArgs e) { }

        /// <summary>
        /// The UpdateTitleBarWindowButtonsVisibility
        /// </summary>
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

        #endregion

        private void ClickThroughButton_Click(object sender, RoutedEventArgs e)
        {
            this.SetExtendedWindowStyle(
                ExtendedWindowStyle.Transparent | ExtendedWindowStyle.Layered
            );
        }
    }
}
