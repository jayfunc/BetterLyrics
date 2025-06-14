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
        private readonly GlobalViewModel _globalViewModel =
            Ioc.Default.GetService<GlobalViewModel>()!;

        public OverlayWindow()
        {
            this.InitializeComponent();

            // Hide titlebar
            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;

            // Hide border
            this.SetWindowStyle(WindowStyle.Popup);

            //this.SetExtendedWindowStyle(
            //    ExtendedWindowStyle.Layered | ExtendedWindowStyle.Transparent
            //);

            // Hide from taskbar and alt-tab
            this.SetIsShownInSwitchers(false);

            SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(BackdropType.Transparent);

            // Transparent window
            // SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(BackdropType.DesktopAcrylic);

            // Stretch to screen width
            //this.CenterOnScreen();
            //var screenWidth = AppWindow.Position.X * 2 + AppWindow.Size.Width;
            //AppWindow.Move(new Windows.Graphics.PointInt32(0, 0));
            //AppWindow.Resize(new Windows.Graphics.SizeInt32(screenWidth, 72));

            // Always on top
            // ((OverlappedPresenter)AppWindow.Presenter).IsAlwaysOnTop = true;

            var hwnd = WindowNative.GetWindowHandle(this);

            var windowWatcher = new ForegroundWindowWatcherHelper(
                hwnd,
                onWindowChanged =>
                {
                    UpdateAccentColor(hwnd);
                }
            );

            windowWatcher.Start();

            UpdateAccentColor(hwnd);
        }

        private void UpdateAccentColor(nint hwnd)
        {
            _globalViewModel.ActivatedWindowAccentColor = WindowColorHelper.GetDominantColorBelow(
                hwnd
            );
        }

        public void Navigate(Type type)
        {
            RootFrame.Navigate(type);
        }
    }
}
