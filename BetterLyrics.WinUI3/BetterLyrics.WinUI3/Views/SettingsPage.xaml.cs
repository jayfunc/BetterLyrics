// 2025/6/23 by Zhe Fang

using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.System;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class SettingsPage : Page
    {
        private bool _isUserToggle;

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
                if (toggleSwitch.DataContext is LocalLyricsFolder localLyricsFolder)
                {
                    ViewModel.ToggleLocalLyricsFolder(localLyricsFolder);
                }
            }
        }

        private void LyricsSearchProvidersListView_DragItemsCompleted(
            ListViewBase sender,
            DragItemsCompletedEventArgs args
        )
        {
            ViewModel.OnLyricsSearchProvidersReordered();
        }

        private void LyricsSearchProviderToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {
                if (toggleSwitch.DataContext is LyricsSearchProviderInfo providerInfo)
                {
                    ViewModel.ToggleLyricsSearchProvider(providerInfo);
                }
            }
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
            ViewModel.RemoveFolderAsync((LocalLyricsFolder)(sender as HyperlinkButton)!.Tag);
        }

        private void MediaSourceProviderToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {
                if (toggleSwitch.DataContext is MediaSourceProviderInfo providerInfo)
                {
                    ViewModel.ToggleMediaSourceProvider(providerInfo);
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

        private void TipContainerCenter_Loaded(object sender, RoutedEventArgs e)
        {
            App.Current.SettingsWindowNotificationPanel = TipContainerCenter;
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
            ViewModel.OnAlbumArtSearchProvidersReordered();
        }

        private void AlbumArtSearchProviderToggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleSwitch toggleSwitch)
            {
                if (toggleSwitch.DataContext is AlbumArtSearchProviderInfo providerInfo)
                {
                    ViewModel.ToggleAlbumArtSearchProvider(providerInfo);
                }
            }
        }
    }
}
