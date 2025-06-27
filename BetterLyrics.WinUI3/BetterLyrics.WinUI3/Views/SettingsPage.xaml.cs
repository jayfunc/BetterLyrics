// 2025/6/23 by Zhe Fang

using BetterInAppLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Enums;
using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.Services;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingsPage"/> class.
        /// </summary>
        public SettingsPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<SettingsViewModel>();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the LyricsSettingsControlViewModel
        /// </summary>
        public LyricsSettingsControlViewModel LyricsSettingsControlViewModel =>
            Ioc.Default.GetRequiredService<LyricsSettingsControlViewModel>();

        /// <summary>
        /// Gets the ViewModel
        /// </summary>
        public SettingsViewModel ViewModel => (SettingsViewModel)DataContext;

        #endregion

        #region Methods

        /// <summary>
        /// The LocalLyricsFolderToggleSwitch_Toggled
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/></param>
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

        /// <summary>
        /// The LyricsSearchProvidersListView_DragItemsCompleted
        /// </summary>
        /// <param name="sender">The sender<see cref="ListViewBase"/></param>
        /// <param name="args">The args<see cref="DragItemsCompletedEventArgs"/></param>
        private void LyricsSearchProvidersListView_DragItemsCompleted(
            ListViewBase sender,
            DragItemsCompletedEventArgs args
        )
        {
            ViewModel.OnLyricsSearchProvidersReordered();
        }

        /// <summary>
        /// The LyricsSearchProviderToggleSwitch_Toggled
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="RoutedEventArgs"/></param>
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

        /// <summary>
        /// The NavView_SelectionChanged
        /// </summary>
        /// <param name="sender">The sender<see cref="NavigationView"/></param>
        /// <param name="args">The args<see cref="NavigationViewSelectionChangedEventArgs"/></param>
        private void NavView_SelectionChanged(
            NavigationView sender,
            NavigationViewSelectionChangedEventArgs args
        )
        {
            ViewModel.NavViewSelectedItemTag = (args.SelectedItem as NavigationViewItem)!.Tag;
        }

        /// <summary>
        /// The SettingsPageOpenPathButton_Click
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="Microsoft.UI.Xaml.RoutedEventArgs"/></param>
        private void SettingsPageOpenPathButton_Click(
            object sender,
            Microsoft.UI.Xaml.RoutedEventArgs e
        )
        {
            ViewModel.OpenMusicFolder((LocalLyricsFolder)(sender as HyperlinkButton)!.Tag);
        }

        /// <summary>
        /// The SettingsPageRemovePathButton_Click
        /// </summary>
        /// <param name="sender">The sender<see cref="object"/></param>
        /// <param name="e">The e<see cref="Microsoft.UI.Xaml.RoutedEventArgs"/></param>
        private void SettingsPageRemovePathButton_Click(
            object sender,
            Microsoft.UI.Xaml.RoutedEventArgs e
        )
        {
            ViewModel.RemoveFolderAsync((LocalLyricsFolder)(sender as HyperlinkButton)!.Tag);
        }

        #endregion
    }
}
