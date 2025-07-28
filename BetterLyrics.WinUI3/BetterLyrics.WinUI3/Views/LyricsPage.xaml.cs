// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class LyricsPage : Page
    {
        private readonly ISettingsService _settingsService = Ioc.Default.GetRequiredService<ISettingsService>();
        private readonly IPlaybackService _playbackService = Ioc.Default.GetRequiredService<IPlaybackService>();

        public LyricsPageViewModel ViewModel => (LyricsPageViewModel)DataContext;

        public LyricsPage()
        {
            this.InitializeComponent();

            DataContext = Ioc.Default.GetRequiredService<LyricsPageViewModel>();
        }

        private void WelcomeTeachingTip_Closed(TeachingTip sender, TeachingTipClosedEventArgs args)
        {
            ViewModel.IsFirstRun = false;
        }

        private void LyricsOnlyRadioButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DisplayType = LyricsDisplayType.LyricsOnly;
            _settingsService.DisplayType = ViewModel.DisplayType;
        }

        private void AlbumArtOnlyRadioButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DisplayType = LyricsDisplayType.AlbumArtOnly;
            _settingsService.DisplayType = ViewModel.DisplayType;
        }

        private void SplitViewRadioButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DisplayType = LyricsDisplayType.SplitView;
            _settingsService.DisplayType = ViewModel.DisplayType;
        }

        private void PositionOffsetResetButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.PositionOffset = 0;
        }

        private void BottomCommandGrid_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (ViewModel.IsImmersiveMode && BottomCommandGrid.Children.Count != 0)
            {
                ViewModel.BottomCommandGridOpacity = 1f;
            }
            e.Handled = true;
        }

        private void BottomCommandGrid_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (ViewModel.IsImmersiveMode && BottomCommandGrid.Children.Count != 0)
            {
                ViewModel.BottomCommandGridOpacity = 0f;
            }
            e.Handled = true;
        }

        private void DisplayTypeSwitchButton_Click(object sender, RoutedEventArgs e)
        {
            DisplayTypeSwitchFlyout.ShowAt(BottomRightCommandStackPanel);
        }

        private void TimelineOffsetButton_Click(object sender, RoutedEventArgs e)
        {
            TimelineOffsetFlyout.ShowAt(BottomLeftCommandStackPanel);
        }

        private void TranslationButton_Click(object sender, RoutedEventArgs e)
        {
            TranslationFlyout.ShowAt(BottomRightCommandStackPanel);
        }

        private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width < 500 || e.NewSize.Height < 100)
            {
                if (BottomCommandGrid.Children.Count != 0)
                {
                    BottomCommandGrid.Children.Remove(BottomCommandContent);
                    BottomCommandFlyoutContainer.Children.Add(BottomCommandContent);
                }
                BottomCommandFlyoutTriggerHint.Translation = new Vector3(0, 0, 0);
            }
            else
            {
                if (BottomCommandFlyoutContainer.Children.Count != 0)
                {
                    BottomCommandFlyout.Hide();
                    BottomCommandFlyoutContainer.Children.Remove(BottomCommandContent);
                    BottomCommandGrid.Children.Add(BottomCommandContent);
                }
                BottomCommandFlyoutTriggerHint.Translation = new Vector3(0, 12, 0);
            }
        }

        //private void VolumeButton_Click(object sender, RoutedEventArgs e)
        //{
        //    VolumeFlyout.ShowAt(BottomRightCommandStackPanel);
        //}

        private void BottomCommandFlyoutTrigger_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (ViewModel.IsImmersiveMode && BottomCommandFlyoutContainer.Children.Count != 0)
            {
                ViewModel.BottomCommandFlyoutTriggerOpacity = 1f;
            }
        }

        private void BottomCommandFlyoutTrigger_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
        {
            if (ViewModel.IsImmersiveMode && BottomCommandFlyoutContainer.Children.Count != 0)
            {
                ViewModel.BottomCommandFlyoutTriggerOpacity = 0f;
            }
        }

        private void BottomCommandFlyoutTrigger_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (BottomCommandFlyoutContainer.Children.Count != 0)
            {
                BottomCommandFlyout.ShowAt(BottomCommandFlyoutTrigger);
            }
        }

        private void TimelineSliderOverlay_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            _playbackService.ChangePosition(TimelineSlider.Value);
        }
    }
}
