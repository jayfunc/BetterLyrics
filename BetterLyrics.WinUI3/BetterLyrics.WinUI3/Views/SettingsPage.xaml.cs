// 2025/6/23 by Zhe Fang

using BetterInAppLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace BetterLyrics.WinUI3.Views
{
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<SettingsPageViewModel>();
        }

        public LyricsSettingsControlViewModel LyricsSettingsControlViewModel =>
            Ioc.Default.GetRequiredService<LyricsSettingsControlViewModel>();

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
    }
}
