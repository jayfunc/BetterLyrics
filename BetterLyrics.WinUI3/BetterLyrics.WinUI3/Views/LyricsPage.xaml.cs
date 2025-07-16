// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class LyricsPage : Page
    {
        public LyricsPage()
        {
            this.InitializeComponent();

            DataContext = Ioc.Default.GetService<LyricsPageViewModel>();
        }

        public LyricsPageViewModel ViewModel => (LyricsPageViewModel)DataContext;

        private void WelcomeTeachingTip_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
        {
            ViewModel.IsFirstRun = false;
        }

        private void LyricsOnlyRadioButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DisplayType = LyricsDisplayType.LyricsOnly;
        }

        private void AlbumArtOnlyRadioButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DisplayType = LyricsDisplayType.AlbumArtOnly;
        }

        private void SplitViewRadioButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DisplayType = LyricsDisplayType.SplitView;
        }

        private void PositionOffsetResetButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.PositionOffset = 0;
        }

        private void BottomCommandGrid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (ViewModel.IsImmersiveMode)
            {
                ViewModel.BottomCommandGridOpacity = 1f;
            }
        }

        private void BottomCommandGrid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (ViewModel.IsImmersiveMode)
            {
                ViewModel.BottomCommandGridOpacity = 0f;
            }
        }

        private void DisplayTypeSwitchButton_Click(object sender, RoutedEventArgs e)
        {
            DisplayTypeSwitchFlyout.ShowAt(BottomRightCommandStackPanel);
        }

        private void TimelineOffsetButton_Click(object sender, RoutedEventArgs e)
        {
            TimelineOffsetFlyout.ShowAt(BottomRightCommandStackPanel);
        }
    }
}
