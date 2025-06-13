using System;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
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
        public OverlayWindow()
        {
            this.InitializeComponent();

            // Hide titlebar
            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;

            // Hide border
            this.SetWindowStyle(WindowStyle.Popup | WindowStyle.Visible);

            // Hide from taskbar and alt-tab
            this.SetIsShownInSwitchers(false);

            // Transparent window
            SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(BackdropType.Transparent);

            // Stretch to screen width
            this.CenterOnScreen();
            var screenWidth = AppWindow.Position.X * 2 + AppWindow.Size.Width;
            AppWindow.Move(new Windows.Graphics.PointInt32(0, 0));
            AppWindow.Resize(new Windows.Graphics.SizeInt32(screenWidth, 72));

            // Always on top
            ((OverlappedPresenter)AppWindow.Presenter).IsAlwaysOnTop = true;
        }

        public void Navigate(Type type)
        {
            RootFrame.Navigate(type);
        }
    }
}
