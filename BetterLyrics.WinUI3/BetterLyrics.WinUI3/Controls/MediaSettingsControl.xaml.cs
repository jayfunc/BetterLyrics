using BetterLyrics.WinUI3.Models;
using BetterLyrics.WinUI3.ViewModels;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
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
            ViewModel.RemoveFolderAsync((MediaFolder)(sender as HyperlinkButton)!.Tag);
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
    }
}
