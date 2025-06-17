using System;
using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class InAppLyricsPage : Page
    {
        public InAppLyricsPageViewModel ViewModel => (InAppLyricsPageViewModel)DataContext;

        public InAppLyricsPage()
        {
            this.InitializeComponent();

            DataContext = Ioc.Default.GetService<InAppLyricsPageViewModel>();
        }

        private void WelcomeTeachingTip_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
        {
            ViewModel.IsFirstRun = false;
        }

        private void CoverArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CoverImageGrid.Width = CoverImageGrid.Height = Math.Min(
                CoverArea.ActualWidth,
                CoverArea.ActualHeight
            );
        }

        private void BottomCommandGrid_PointerEntered(
            object sender,
            Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e
        )
        {
            if (ViewModel.IsImmersiveMode && BottomCommandGrid.Opacity == 0)
                BottomCommandGrid.Opacity = .5;
        }

        private void BottomCommandGrid_PointerExited(
            object sender,
            Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e
        )
        {
            if (ViewModel.IsImmersiveMode && BottomCommandGrid.Opacity == .5)
                BottomCommandGrid.Opacity = 0;
        }

        private void LyricsPlaceholderGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewModel.LimitedLineWidth = e.NewSize.Width;
        }

        private void OpenMatchedFileButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenMatchedFileFolderInFileExplorer((string)(sender as HyperlinkButton)!.Tag);
        }

        private void CoverImageGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewModel.CoverImageGridActualHeight = e.NewSize.Height;
        }

        private void DesktopLyricsToggleButton_Click(object sender, RoutedEventArgs e)
        {
            WindowHelper.GetWindowForElement(this).Close();
            WindowHelper.OpenDesktopLyricsWindow();
        }
    }
}
