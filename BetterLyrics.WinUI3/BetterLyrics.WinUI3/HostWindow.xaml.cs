using System;
using System.Diagnostics;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Messages;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services.Settings;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Views;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HostWindow : Window
    {
        public HostViewModel ViewModel { get; set; } = Ioc.Default.GetService<HostViewModel>()!;
        public GlobalViewModel GlobalSettingsViewModel { get; set; } =
            Ioc.Default.GetService<GlobalViewModel>()!;

        private readonly ILogger<HostWindow> _logger = Ioc.Default.GetService<
            ILogger<HostWindow>
        >()!;

        private WindowStyle _savedWindowStyle;
        private DisplayType _savedDisplayType;
        private Windows.Graphics.SizeInt32 _savedWindowSize;
        private Windows.Graphics.PointInt32 _savedWindowPos;

        public HostWindow()
        {
            this.InitializeComponent();

            AppWindow.Changed += AppWindow_Changed;

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(TopCommandGrid);

            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;

            UpdateBackdrop(GlobalSettingsViewModel.BackdropType);

            ViewModel.UpdateTitleBarStyle(GlobalSettingsViewModel.TitleBarType);

            WeakReferenceMessenger.Default.Register<SystemBackdropChangedMessage>(
                this,
                (r, m) =>
                {
                    UpdateBackdrop(m.Value);
                }
            );

            WeakReferenceMessenger.Default.Register<IsImmersiveModeChangedMessage>(
                this,
                (r, m) =>
                {
                    TopCommandGrid.Opacity = m.Value ? 0 : 1;
                }
            );

            WeakReferenceMessenger.Default.Register<IsDesktopModeChangedMessage>(
                this,
                (r, m) =>
                {
                    if (RootFrame.SourcePageType != typeof(MainPage))
                        return;

                    var overlappedPresenter = (OverlappedPresenter)AppWindow.Presenter;

                    if (m.Value)
                    {
                        _savedWindowStyle = this.GetWindowStyle();
                        _savedDisplayType = GlobalSettingsViewModel.DisplayType;
                        _savedWindowSize = AppWindow.Size;
                        _savedWindowPos = AppWindow.Position;

                        GlobalSettingsViewModel.DisplayType = DisplayType.LyricsOnly;
                        this.SetWindowStyle(WindowStyle.Popup | WindowStyle.Visible);
                        SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(
                            BackdropType.Transparent
                        );
                        this.CenterOnScreen();
                        var screenWidth = AppWindow.Position.X * 2 + AppWindow.Size.Width;
                        AppWindow.Move(new Windows.Graphics.PointInt32(0, 0));
                        AppWindow.Resize(new Windows.Graphics.SizeInt32(screenWidth, 64));

                        TopCommandGrid.Visibility = Visibility.Collapsed;
                        overlappedPresenter.IsAlwaysOnTop = true;
                    }
                    else
                    {
                        GlobalSettingsViewModel.DisplayType = _savedDisplayType;
                        this.SetWindowStyle(_savedWindowStyle);
                        SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(
                            GlobalSettingsViewModel.BackdropType
                        );
                        this.AppWindow.Move(_savedWindowPos);
                        this.AppWindow.Resize(_savedWindowSize);

                        TopCommandGrid.Visibility = Visibility.Visible;
                        overlappedPresenter.IsAlwaysOnTop = false;
                    }
                }
            );
        }

        private void UpdateBackdrop(BackdropType? backdropType)
        {
            if (RootFrame.SourcePageType == typeof(MainPage))
                SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(
                    backdropType ?? GlobalSettingsViewModel.BackdropType
                );
            else
                SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(BackdropType.Mica);
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
                        MiniFlyoutItem.Visibility =
                            Visibility.Collapsed;
                    UnMiniFlyoutItem.Visibility = Visibility.Visible;
                    break;
                case AppWindowPresenterKind.FullScreen:
                    MinimiseButton.Visibility =
                        MaximiseButton.Visibility =
                        RestoreButton.Visibility =
                        AOTFlyoutItem.Visibility =
                        MiniFlyoutItem.Visibility =
                        UnMiniFlyoutItem.Visibility =
                            Visibility.Collapsed;
                    FullScreenFlyoutItem.IsChecked = true;
                    break;
                case AppWindowPresenterKind.Overlapped:
                    var overlappedPresenter = (OverlappedPresenter)AppWindow.Presenter;
                    MinimiseButton.Visibility =
                        AOTFlyoutItem.Visibility =
                        MiniFlyoutItem.Visibility =
                        FullScreenFlyoutItem.Visibility =
                            Visibility.Visible;
                    FullScreenFlyoutItem.IsChecked = false;
                    AOTFlyoutItem.IsChecked = overlappedPresenter.IsAlwaysOnTop;
                    UnMiniFlyoutItem.Visibility = Visibility.Collapsed;

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
                    WeakReferenceMessenger.Default.Send(
                        new ShowNotificatonMessage(
                            new Models.Notification(
                                App.ResourceLoader!.GetString("BaseWindowEnterFullScreenHint"),
                                isForeverDismissable: true,
                                relatedSettingsKeyName: SettingsKeys.NeverShowEnterFullScreenMessage
                            )
                        )
                    );
                    break;
                default:
                    break;
            }
        }

        private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (
                AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen
                && e.Key == Windows.System.VirtualKey.Escape
            )
                AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        }

        private void MiniFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            AppWindow.SetPresenter(AppWindowPresenterKind.CompactOverlay);
        }

        private void UnMiniFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
        }
    }
}
