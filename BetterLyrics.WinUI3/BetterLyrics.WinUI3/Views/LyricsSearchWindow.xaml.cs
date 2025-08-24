using BetterLyrics.WinUI3.Helper;
using Microsoft.UI.Windowing;
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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LyricsSearchWindow : Window
    {
        public LyricsSearchWindow()
        {
            InitializeComponent();

            Title = App.ResourceLoader?.GetString("LyricsSearchPageTitle");
            AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
            AppWindow.SetIcons();

            AppWindow.Closing += AppWindow_Closing;

            SystemBackdrop = SystemBackdropHelper.CreateSystemBackdrop(Enums.BackdropType.Transparent);
        }

        private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            WindowHelper.CloseWindow<LyricsSearchWindow>();
        }

    }
}
