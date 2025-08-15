// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Controls;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models.Settings;
using BetterLyrics.WinUI3.Services.MediaSessionsService;
using BetterLyrics.WinUI3.Services.SettingsService;
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
        private readonly IMediaSessionsService _mediaSessionsService = Ioc.Default.GetRequiredService<IMediaSessionsService>();

        public LyricsPageViewModel ViewModel => (LyricsPageViewModel)DataContext;

        public LyricsPage()
        {
            this.InitializeComponent();

            DataContext = Ioc.Default.GetRequiredService<LyricsPageViewModel>();
        }

        private void LyricsOnlyRadioButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DisplayType = LyricsDisplayType.LyricsOnly;
            _settingsService.AppSettings.GeneralSettings.DisplayType = ViewModel.DisplayType;
        }

        private void AlbumArtOnlyRadioButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DisplayType = LyricsDisplayType.AlbumArtOnly;
            _settingsService.AppSettings.GeneralSettings.DisplayType = ViewModel.DisplayType;
        }

        private void SplitViewRadioButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.DisplayType = LyricsDisplayType.SplitView;
            _settingsService.AppSettings.GeneralSettings.DisplayType = ViewModel.DisplayType;
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

        private void PlaybackSettingsShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            PlaybackSettingsFlyout.Content = new PlaybackSettingsControl
            {
                MaxHeight = 500,
                MaxWidth = 850,
            };
            PlaybackSettingsFlyout.ShowAt(BottomRightCommandStackPanel);
        }

        private void RootGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width < 450 || e.NewSize.Height < 100)
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
            _mediaSessionsService.ChangePosition(TimelineSlider.Value);
        }

        private void PlaybackSettingsFlyout_Closed(object sender, object e)
        {
            PlaybackSettingsFlyout.Content = null;
        }

        private void LyricsSettingsFlyout_Closed(object sender, object e)
        {
            LyricsSettingsFlyout.Content = null;
        }

        private void LyricsSettingsShortcutButton_Click(object sender, RoutedEventArgs e)
        {
            LyricsSettingsFlyout.Content = new AllLyricsSettingsControl
            {
                MaxHeight = 500,
                MaxWidth = 850,
            };
            LyricsSettingsFlyout.ShowAt(BottomRightCommandStackPanel);
        }
    }
}
