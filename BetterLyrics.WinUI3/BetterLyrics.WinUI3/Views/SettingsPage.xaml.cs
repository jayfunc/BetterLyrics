using System.Threading.Tasks;
using BetterInAppLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
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
    public sealed partial class SettingsPage : Page
    {
        public SettingsViewModel ViewModel => (SettingsViewModel)DataContext;
        public LyricsSettingsControlViewModel LyricsSettingsControlViewModel =>
            Ioc.Default.GetRequiredService<LyricsSettingsControlViewModel>();

        public SettingsPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<SettingsViewModel>();
        }

        private void SettingsPageOpenPathButton_Click(
            object sender,
            Microsoft.UI.Xaml.RoutedEventArgs e
        )
        {
            ViewModel.OpenMusicFolder((string)(sender as HyperlinkButton)!.Tag);
        }

        private void SettingsPageRemovePathButton_Click(
            object sender,
            Microsoft.UI.Xaml.RoutedEventArgs e
        )
        {
            ViewModel.RemoveFolderAsync((string)(sender as HyperlinkButton)!.Tag);
        }

        private void NavView_SelectionChanged(
            NavigationView sender,
            NavigationViewSelectionChangedEventArgs args
        )
        {
            ViewModel.NavViewSelectedItemTag = (args.SelectedItem as NavigationViewItem)!.Tag;
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

        private void LyricsSearchProvidersSettingsExpander_Expanded(
            object sender,
            System.EventArgs e
        )
        {
            if (LyricsSearchProvidersListView != null)
            {
                LyricsSearchProvidersListView.Visibility = Visibility.Visible;
                LyricsSearchProvidersListView.Opacity = 1;
            }
        }

        private async void LyricsSearchProvidersSettingsExpander_Collapsed(
            object sender,
            System.EventArgs e
        )
        {
            if (LyricsSearchProvidersListView != null)
            {
                LyricsSearchProvidersListView.Opacity = 0;
                await Task.Delay(300);
                LyricsSearchProvidersListView.Visibility = Visibility.Collapsed;
            }
        }

        private void LyricsSearchProvidersListView_DragItemsCompleted(
            ListViewBase sender,
            DragItemsCompletedEventArgs args
        )
        {
            ViewModel.OnLyricsSearchProvidersReordered();
        }
    }
}
