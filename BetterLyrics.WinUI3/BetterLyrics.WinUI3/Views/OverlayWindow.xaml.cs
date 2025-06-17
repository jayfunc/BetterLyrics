using System;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class OverlayWindow : Window
    {
        public OverlayWindowViewModel ViewModel =>
            Ioc.Default.GetRequiredService<OverlayWindowViewModel>();

        private readonly bool _listenOnActivatedWindowChange;

        public OverlayWindow(
            bool alwaysOnTop = true,
            bool clickThrough = false,
            bool listenOnActivatedWindowChange = false
        )
        {
            this.InitializeComponent();

            _listenOnActivatedWindowChange = listenOnActivatedWindowChange;

            // Hide titlebar
            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;

            // Hide border
            this.SetWindowStyle(WindowStyle.Popup);

            if (clickThrough)
                this.SetExtendedWindowStyle(
                    ExtendedWindowStyle.Transparent | ExtendedWindowStyle.Layered
                );

            // Hide from taskbar and alt-tab
            this.SetIsShownInSwitchers(false);

            SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(BackdropType.Transparent);

            if (alwaysOnTop)
                ((OverlappedPresenter)AppWindow.Presenter).IsAlwaysOnTop = true;

            if (listenOnActivatedWindowChange)
            {
                var hwnd = WindowNative.GetWindowHandle(this);
                var windowWatcher = new ForegroundWindowWatcherHelper(
                    hwnd,
                    onWindowChanged =>
                    {
                        ViewModel.UpdateAccentColor(hwnd);
                    }
                );
                Closed += (_, _) =>
                {
                    windowWatcher.Stop();
                };
                windowWatcher.Start();
            }
        }

        public void Navigate(Type type)
        {
            RootFrame.Navigate(type);
        }

        private void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (_listenOnActivatedWindowChange)
            {
                var hwnd = WindowNative.GetWindowHandle(this);
                ViewModel.UpdateAccentColor(hwnd);
            }
        }
    }
}
