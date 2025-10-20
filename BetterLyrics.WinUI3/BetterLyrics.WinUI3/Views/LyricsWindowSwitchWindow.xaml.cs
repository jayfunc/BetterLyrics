using BetterLyrics.WinUI3.Helper;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LyricsWindowSwitchWindow : Window
    {
        public LyricsWindowSwitchWindow()
        {
            InitializeComponent();

            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Collapsed;
            this.CenterOnScreen();
            this.SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(Enums.BackdropType.Transparent);
            this.SetWindowStyle(WindowStyle.Popup | WindowStyle.Visible);
            AppWindow.IsShownInSwitchers = false;
            this.SetIsAlwaysOnTop(true);
            SetTitleBar(PlaceholderGrid);
        }
    }
}
