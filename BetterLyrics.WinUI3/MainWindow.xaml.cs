using System;
using System.Diagnostics;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Services.Settings;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.Behaviors;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using WinRT;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3 {
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window {

        private readonly OverlappedPresenter _presenter;
        private bool _isMiniMode = false;

        public static StackedNotificationsBehavior? StackedNotificationsBehavior { get; private set; }

        private readonly ISettingsService _settingsService;

        public MainWindow() {
            this.InitializeComponent();

            _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
            RootGrid.RequestedTheme = _settingsService.Theme;
            SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(_settingsService.BackdropType);

            AppWindow.SetIcon("white_round.ico");
            StackedNotificationsBehavior = NotificationQueue;

            _presenter = (OverlappedPresenter)AppWindow.Presenter;

            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Collapsed;
            SetTitleBar(TopCommandGrid);

            RootFrame.Navigate(typeof(MainPage));
        }

        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e) {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e) {
            RootFrame.Navigate(typeof(SettingsPage));
        }

        private void BackButton_Click(object sender, RoutedEventArgs e) {
            RootFrame.GoBack();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) {
            Application.Current.Exit();
        }

        private void MaximiseButton_Click(object sender, RoutedEventArgs e) {
            _presenter.Maximize();
            //MaximiseButton.Visibility = Visibility.Collapsed;
            //RestoreButton.Visibility = Visibility.Visible;
        }

        private void MinimiseButton_Click(object sender, RoutedEventArgs e) {
            _presenter.Minimize();
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e) {
            _presenter.Restore();
            //MaximiseButton.Visibility = Visibility.Visible;
            //RestoreButton.Visibility = Visibility.Collapsed;
        }

        private void Window_SizeChanged(object sender, WindowSizeChangedEventArgs args) {
            if (_presenter.State == OverlappedPresenterState.Maximized) {
                MaximiseButton.Visibility = Visibility.Collapsed;
                RestoreButton.Visibility = Visibility.Visible;
            } else if (_presenter.State == OverlappedPresenterState.Restored) {
                MaximiseButton.Visibility = Visibility.Visible;
                RestoreButton.Visibility = Visibility.Collapsed;
            }
        }

        private void MiniButton_Click(object sender, RoutedEventArgs e) {
            AppWindow.Resize(new Windows.Graphics.SizeInt32(144, 48));
            MiniButton.Visibility = Visibility.Collapsed;
            UnminiButton.Visibility = Visibility.Visible;
            MinimiseButton.Visibility = Visibility.Collapsed;
            MaximiseButton.Visibility = Visibility.Collapsed;
            RestoreButton.Visibility = Visibility.Collapsed;
            CloseButton.Visibility = Visibility.Collapsed;
        }

        private void UnminiButton_Click(object sender, RoutedEventArgs e) {
            AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 600));
            MiniButton.Visibility = Visibility.Visible;
            UnminiButton.Visibility = Visibility.Collapsed;
            MinimiseButton.Visibility = Visibility.Visible;
            MaximiseButton.Visibility = Visibility.Visible;
            RestoreButton.Visibility = Visibility.Collapsed;
            CloseButton.Visibility = Visibility.Visible;
        }
    }
}
