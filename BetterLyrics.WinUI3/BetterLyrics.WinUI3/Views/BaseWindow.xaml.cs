using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Settings;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Behaviors;
using DevWinUI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Windows.UI.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BaseWindow : Window
    {
        public BaseWindowModel WindowModel { get; set; }

        private SettingsService SettingsService { get; set; }

        public static StackedNotificationsBehavior? StackedNotificationsBehavior
        {
            get;
            private set;
        }

        public BaseWindow()
        {
            this.InitializeComponent();

            StackedNotificationsBehavior = NotificationQueue;

            WindowModel = Ioc.Default.GetService<BaseWindowModel>()!;

            SettingsService = Ioc.Default.GetService<SettingsService>()!;
            SettingsService.PropertyChanged += SettingsService_PropertyChanged;

            SettingsService_PropertyChanged(
                null,
                new System.ComponentModel.PropertyChangedEventArgs(nameof(SettingsService.Theme))
            );
            SettingsService_PropertyChanged(
                null,
                new System.ComponentModel.PropertyChangedEventArgs(
                    nameof(SettingsService.BackdropType)
                )
            );
            SettingsService_PropertyChanged(
                null,
                new System.ComponentModel.PropertyChangedEventArgs(
                    nameof(SettingsService.TitleBarType)
                )
            );

            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
            SetTitleBar(TopCommandGrid);
        }

        private void SettingsService_PropertyChanged(
            object? sender,
            System.ComponentModel.PropertyChangedEventArgs e
        )
        {
            switch (e.PropertyName)
            {
                case nameof(Services.Settings.SettingsService.Theme):
                    RootGrid.RequestedTheme = (ElementTheme)SettingsService.Theme;
                    break;
                case nameof(Services.Settings.SettingsService.BackdropType):
                    SystemBackdrop = null;
                    SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(
                        (BackdropType)SettingsService.BackdropType
                    );
                    break;
                case nameof(Services.Settings.SettingsService.TitleBarType):
                    switch ((TitleBarType)SettingsService.TitleBarType)
                    {
                        case TitleBarType.Compact:
                            TopCommandGrid.Height = (double)
                                App.Current.Resources["TitleBarCompactHeight"];
                            AppLogoImageIcon.Height = 18;
                            WindowModel.TitleBarFontSize = 11;
                            break;
                        case TitleBarType.Extended:
                            TopCommandGrid.Height = (double)
                                App.Current.Resources["TitleBarExpandedHeight"];
                            AppLogoImageIcon.Height = 20;
                            WindowModel.TitleBarFontSize = 14;
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
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
            if (RootFrame.CurrentSourcePageType == typeof(MainPage))
            {
                App.Current.Exit();
            }
            else if (RootFrame.CurrentSourcePageType == typeof(SettingsPage))
            {
                App.Current.SettingsWindow!.AppWindow.Hide();
            }
        }

        private void MaximiseButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Maximize();
                UpdateTitleBarWindowButtonsVisibility();
            }
        }

        private void MinimiseButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Minimize();
                UpdateTitleBarWindowButtonsVisibility();
            }
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.Restore();
                UpdateTitleBarWindowButtonsVisibility();
            }
        }

        private void UpdateTitleBarWindowButtonsVisibility()
        {
            if (AppWindow.Presenter is OverlappedPresenter overlappedPresenter)
            {
                MinimiseButton.Visibility = AOTFlyoutItem.Visibility = Visibility.Visible;

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
            else if (AppWindow.Presenter is FullScreenPresenter)
            {
                MinimiseButton.Visibility =
                    MaximiseButton.Visibility =
                    RestoreButton.Visibility =
                    AOTFlyoutItem.Visibility =
                        Visibility.Collapsed;
            }
        }

        private void RootFrame_Navigated(object sender, NavigationEventArgs e)
        {
            AppWindow.Title = Title = App.ResourceLoader!.GetString(
                $"{e.SourcePageType.Name}Title"
            );
        }

        private void AOTFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (AppWindow.Presenter is OverlappedPresenter presenter)
                AOTFlyoutItem.IsChecked = presenter.IsAlwaysOnTop = !presenter.IsAlwaysOnTop;
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
                    StackedNotificationsBehavior?.Show(
                        App.ResourceLoader!.GetString("BaseWindowEnterFullScreenHint"),
                        AnimationHelper.StackedNotificationsShowingDuration
                    );
                    break;
                default:
                    break;
            }

            UpdateTitleBarWindowButtonsVisibility();

            FullScreenFlyoutItem.IsChecked =
                AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen;
        }

        private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (
                AppWindow.Presenter is FullScreenPresenter
                && e.Key == Windows.System.VirtualKey.Escape
            )
                AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        }
    }
}
