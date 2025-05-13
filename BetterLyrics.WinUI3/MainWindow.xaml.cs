using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using BetterLyrics.WinUI3.Views;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using WinRT;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly ISystemBackdropControllerWithTargets _backdropController;
        private readonly ICompositionSupportsSystemBackdrop _backdropTarget;
        private static readonly SystemBackdropConfiguration _systemBackdropConfiguration = new SystemBackdropConfiguration
        {
            IsInputActive = true,
            Theme = SystemBackdropTheme.Default
        };

        public MainWindow()
        {
            this.InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.PreferredHeightOption = Microsoft.UI.Windowing.TitleBarHeightOption.Collapsed;
            SetTitleBar(Draggable);

            _backdropTarget = this.As<ICompositionSupportsSystemBackdrop>();
            _backdropController = new DesktopAcrylicController
            {
                LuminosityOpacity = 0.0f,
                TintOpacity = 0.0f,
            };
            _backdropController.AddSystemBackdropTarget(_backdropTarget);
            _backdropController.SetSystemBackdropConfiguration(_systemBackdropConfiguration);

            RootFrame.Navigate(typeof(MainPage));
        }

        private void RootFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }
    }
}
