// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Helper;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.ViewModels.SettingsPageViewModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.System;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<SettingsPageViewModel>();
        }

        public SettingsPageViewModel ViewModel => (SettingsPageViewModel)DataContext;

        private void LocalLyricsFolderToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {
                if (toggleSwitch.DataContext is LocalMediaFolder localLyricsFolder)
                {
                    ViewModel.ToggleLocalLyricsFolder();
                }
            }
        }

        private void LyricsSearchProvidersListView_DragItemsCompleted(
            ListViewBase sender,
            DragItemsCompletedEventArgs args
        )
        {
            ViewModel.BroadcastMediaSourceProvidersInfoChanged();
        }

        private void LyricsSearchProviderToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ViewModel.BroadcastMediaSourceProvidersInfoChanged();
        }

        private void NavView_SelectionChanged(
            NavigationView sender,
            NavigationViewSelectionChangedEventArgs args
        )
        {
            ViewModel.NavViewSelectedItemTag = (args.SelectedItem as NavigationViewItem)!.Tag;
        }

        private void SettingsPageRemovePathButton_Click(
            object sender,
            Microsoft.UI.Xaml.RoutedEventArgs e
        )
        {
            ViewModel.RemoveFolderAsync((LocalMediaFolder)(sender as HyperlinkButton)!.Tag);
        }

        private void MediaSourceProviderToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {
                if (ViewModel.SelectedMediaSourceProvider != null)
                {
                    ViewModel.SelectedMediaSourceProvider.IsEnabled = toggleSwitch.IsOn;
                    ViewModel.BroadcastMediaSourceProvidersInfoChanged();
                }
            }
        }

        private async void LocalFolderHyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is HyperlinkButton button && button.Tag is string uriStr)
            {
                if (Uri.TryCreate(uriStr, UriKind.Absolute, out var uri))
                {
                    await Launcher.LaunchUriAsync(uri);
                }
            }
        }

        private async void AutoStartupToggleSwitch_Loaded(object sender, RoutedEventArgs e)
        {
            AutoStartupToggleSwitch.IsOn = await ViewModel.DetectIsAutoStartupEnabledAsync();
            AutoStartupToggleSwitch.Toggled += AutoStartupToggleSwitch_Toggled;
        }

        private void AutoStartupToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ViewModel.ToggleAutoStartupAsync(AutoStartupToggleSwitch.IsOn);
        }

        private void AlbumArtSearchProvidersListView_DragItemsCompleted(ListViewBase sender, DragItemsCompletedEventArgs args)
        {
            ViewModel.BroadcastMediaSourceProvidersInfoChanged();
        }

        private void AlbumArtSearchProviderToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ViewModel.BroadcastMediaSourceProvidersInfoChanged();
        }

        private void QQGroupButton_Click(object sender, RoutedEventArgs e)
        {
            Launcher.LaunchUriAsync(new Uri(Constants.Link.QQGroupUrl));
        }

        private void DiscodGroupButton_Click(object sender, RoutedEventArgs e)
        {
            Launcher.LaunchUriAsync(new Uri(Constants.Link.DiscordUrl));
        }

        private void TelegramGroupButton_Click(object sender, RoutedEventArgs e)
        {
            Launcher.LaunchUriAsync(new Uri(Constants.Link.TelegramUrl));
        }

        private void AutoStartupToggleSwitch_Unloaded(object sender, RoutedEventArgs e)
        {
            AutoStartupToggleSwitch.Toggled -= AutoStartupToggleSwitch_Toggled;
        }

        private void MediaSourceProviderLastFMTrackToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {
                if (ViewModel.SelectedMediaSourceProvider != null)
                {
                    ViewModel.SelectedMediaSourceProvider.IsLastFMTrackEnabled = toggleSwitch.IsOn;
                    ViewModel.BroadcastMediaSourceProvidersInfoChanged();
                }
            }
        }

        private void MediaSourceProviderTimelineSyncThresholdSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            ViewModel.BroadcastMediaSourceProvidersInfoChanged();
        }

        private void MediaSourceProviderPositionOffsetResetButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedMediaSourceProvider?.PositionOffset = 0;
        }

        private void MediaSourceProviderPositionOffsetSlider_ValueChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            ViewModel.BroadcastMediaSourceProvidersInfoChanged();
        }

        private void ResetPositionOffsetOnSongChangedToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ViewModel.BroadcastMediaSourceProvidersInfoChanged();
        }
    }
}
