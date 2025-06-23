// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame
    /// </summary>
    public sealed partial class LyricsPage : Page
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="LyricsPage"/> class.
        /// </summary>
        public LyricsPage()
        {
            this.InitializeComponent();

            DataContext = Ioc.Default.GetService<LyricsPageViewModel>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the ViewModel
        /// </summary>
        public LyricsPageViewModel ViewModel => (LyricsPageViewModel)DataContext;

        #endregion

        #region Methods

        /// <summary>
        /// The BottomCommandGrid_PointerEntered
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="Microsoft.UI.Xaml.Input.PointerRoutedEventArgs"/></param>
        private void BottomCommandGrid_PointerEntered(
            object sender,
            Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e
        )
        {
            if (BottomCommandGrid.Opacity == 0)
                BottomCommandGrid.Opacity = .5;
        }

        /// <summary>
        /// The BottomCommandGrid_PointerExited
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="Microsoft.UI.Xaml.Input.PointerRoutedEventArgs"/></param>
        private void BottomCommandGrid_PointerExited(
            object sender,
            Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e
        )
        {
            if (BottomCommandGrid.Opacity == .5)
                BottomCommandGrid.Opacity = 0;
        }

        /// <summary>
        /// The CoverArea_SizeChanged
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="SizeChangedEventArgs"/></param>
        private void CoverArea_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CoverImageGrid.Width = CoverImageGrid.Height = Math.Min(
                CoverArea.ActualWidth,
                CoverArea.ActualHeight
            );
        }

        /// <summary>
        /// The CoverImageGrid_SizeChanged
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="SizeChangedEventArgs"/></param>
        private void CoverImageGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewModel.CoverImageGridActualHeight = e.NewSize.Height;
        }

        /// <summary>
        /// The LyricsPlaceholderGrid_SizeChanged
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="SizeChangedEventArgs"/></param>
        private void LyricsPlaceholderGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewModel.LimitedLineWidth = e.NewSize.Width;
        }

        /// <summary>
        /// The WelcomeTeachingTip_Closed
        /// </summary>
        /// <param name="sender">The sender<see cref="TeachingTip"/></param>
        /// <param name="args">The args<see cref="TeachingTipClosedEventArgs"/></param>
        private void WelcomeTeachingTip_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
        {
            ViewModel.IsFirstRun = false;
        }

        #endregion
    }
}
