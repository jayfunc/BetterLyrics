using System;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Services.Settings;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI.Behaviors;
using DevWinUI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly OverlappedPresenter _presenter;

        private readonly SettingsService _settingsService;

        public static StackedNotificationsBehavior? StackedNotificationsBehavior
        {
            get;
            private set;
        }

        public MainWindow()
        {
            this.InitializeComponent();

            _settingsService = Ioc.Default.GetService<SettingsService>()!;

            RootGrid.RequestedTheme = (ElementTheme)_settingsService.Theme;
            SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(
                (BackdropType)_settingsService.BackdropType
            );

            WeakReferenceMessenger.Default.Register<ThemeChangedMessage>(
                this,
                (r, m) =>
                {
                    RootGrid.RequestedTheme = m.Value;
                }
            );

            WeakReferenceMessenger.Default.Register<SystemBackdropChangedMessage>(
                this,
                (r, m) =>
                {
                    SystemBackdrop = null;
                    SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(m.Value);
                }
            );

            // AppWindow.SetIcon("white_round.ico");
            StackedNotificationsBehavior = NotificationQueue;

            _presenter = (OverlappedPresenter)AppWindow.Presenter;

            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
            SetTitleBar(TopCommandGrid);
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
                (
                    (RootFrame.Content as MainPage)!.FindChild("LyricsCanvas")
                    as CanvasAnimatedControl
                )!.Paused = true;
                App.Current.Exit();
            }
            else if (RootFrame.CurrentSourcePageType == typeof(SettingsPage))
            {
                App.Current.SettingsWindow!.AppWindow.Hide();
            }
        }

        private void MaximiseButton_Click(object sender, RoutedEventArgs e)
        {
            _presenter.Maximize();
            //MaximiseButton.Visibility = Visibility.Collapsed;
            //RestoreButton.Visibility = Visibility.Visible;
        }

        private void MinimiseButton_Click(object sender, RoutedEventArgs e)
        {
            _presenter.Minimize();
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            _presenter.Restore();
            //MaximiseButton.Visibility = Visibility.Visible;
            //RestoreButton.Visibility = Visibility.Collapsed;
        }

        private void Window_SizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            if (_presenter.State == OverlappedPresenterState.Maximized)
            {
                MaximiseButton.Visibility = Visibility.Collapsed;
                RestoreButton.Visibility = Visibility.Visible;
            }
            else if (_presenter.State == OverlappedPresenterState.Restored)
            {
                MaximiseButton.Visibility = Visibility.Visible;
                RestoreButton.Visibility = Visibility.Collapsed;
            }
        }

        private void MiniButton_Click(object sender, RoutedEventArgs e)
        {
            AppWindow.Resize(new Windows.Graphics.SizeInt32(144, 48));
            MiniButton.Visibility = Visibility.Collapsed;
            UnminiButton.Visibility = Visibility.Visible;
            MinimiseButton.Visibility = Visibility.Collapsed;
            MaximiseButton.Visibility = Visibility.Collapsed;
            RestoreButton.Visibility = Visibility.Collapsed;
            CloseButton.Visibility = Visibility.Collapsed;
        }

        private void UnminiButton_Click(object sender, RoutedEventArgs e)
        {
            AppWindow.Resize(new Windows.Graphics.SizeInt32(800, 600));
            MiniButton.Visibility = Visibility.Visible;
            UnminiButton.Visibility = Visibility.Collapsed;
            MinimiseButton.Visibility = Visibility.Visible;
            MaximiseButton.Visibility = Visibility.Visible;
            RestoreButton.Visibility = Visibility.Collapsed;
            CloseButton.Visibility = Visibility.Visible;
        }

        private void RootFrame_Navigated(object sender, NavigationEventArgs e)
        {
            AppWindow.Title = Title = App.ResourceLoader!.GetString(
                $"{e.SourcePageType.Name}Title"
            );
        }

        private void AOTButton_Click(object sender, RoutedEventArgs e)
        {
            _presenter.IsAlwaysOnTop = !_presenter.IsAlwaysOnTop;
            string prefix;
            if (_presenter.IsAlwaysOnTop)
            {
                prefix = "Show";
            }
            else
            {
                prefix = "Hide";
            }
            (PinnedFontIcon.Resources[$"{prefix}PinnedFontIconStoryboard"] as Storyboard)!.Begin();
        }
    }
}
