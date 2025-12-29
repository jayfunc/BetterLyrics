using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace BetterLyrics.WinUI3.Controls
{
    public sealed partial class MediaSettingsControl : UserControl
    {
        public MediaSettingsControlViewModel ViewModel => (MediaSettingsControlViewModel)DataContext;
        public MediaSettingsControl()
        {
            InitializeComponent();
            DataContext = Ioc.Default.GetRequiredService<MediaSettingsControlViewModel>();
        }

        private void SettingsPageRemovePathButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var folder = (MediaFolder)((FrameworkElement)sender).DataContext;
            ViewModel.RemoveFolder(folder);
        }

        private void SyncNowButton_Click(object sender, RoutedEventArgs e)
        {
            var folder = (MediaFolder)((FrameworkElement)sender).DataContext;
            ViewModel.SyncFolder(folder);
        }
    }
}
