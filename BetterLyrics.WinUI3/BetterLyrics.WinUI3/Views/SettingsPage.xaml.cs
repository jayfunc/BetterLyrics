using BetterInAppLyrics.WinUI3.ViewModels;
using BetterLyrics.WinUI3.Rendering;
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
        public AlbumArtOverlayViewModel AlbumArtRendererSettingsViewModel =>
            Ioc.Default.GetService<AlbumArtOverlayViewModel>()!;
        public InAppLyricsViewModel LyricsRendererSettingsViewModel =>
            Ioc.Default.GetService<InAppLyricsViewModel>()!;
        public GlobalViewModel GlobalSettingsViewModel =>
            Ioc.Default.GetService<GlobalViewModel>()!;
        public AlbumArtViewModel AlbumArtViewModel => Ioc.Default.GetService<AlbumArtViewModel>()!;

        public SettingsPage()
        {
            this.InitializeComponent();
            DataContext = Ioc.Default.GetService<SettingsViewModel>();
        }

        private void SettingsPageOpenPathButton_Click(
            object sender,
            Microsoft.UI.Xaml.RoutedEventArgs e
        )
        {
            SettingsViewModel.OpenMusicFolder((string)(sender as HyperlinkButton)!.Tag);
        }

        private async void SettingsPageRemovePathButton_Click(
            object sender,
            Microsoft.UI.Xaml.RoutedEventArgs e
        )
        {
            await ViewModel.RemoveFolderAsync((string)(sender as HyperlinkButton)!.Tag);
        }
    }
}
